using Npgsql;

namespace ChatApi;

/// <summary>
///  Хранит данные мессенджера в PostgreSQL. Весь SQL приложения собран здесь:
///  ChatService проверяет значения и формулирует правила, а этот класс читает и пишет строки.
/// </summary>
public sealed class PostgresChatStore(NpgsqlDataSource dataSource)
{
    /// <summary>
    ///  Возвращает диалоги пользователя: собеседник и последнее сообщение каждого.
    /// </summary>
    public async Task<IReadOnlyList<DialogSummary>> GetDialogsAsync(string user)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();

        // Собеседник — другой участник диалога, а для диалога с самим собой — сам пользователь.
        // Последнее сообщение — строка с наибольшим id; список идёт от недавних диалогов к старым.
        const string sql = """
            SELECT d.id AS dialog_id, peer.nickname AS peer, m.id AS message_id,
                   author.nickname AS author, m.text, m.sent_at
            FROM dialog_participants AS mine
            JOIN dialogs AS d ON d.id = mine.dialog_id
            LEFT JOIN dialog_participants AS other
                   ON other.dialog_id = d.id AND other.user_id <> mine.user_id
            JOIN users AS peer ON peer.id = COALESCE(other.user_id, mine.user_id)
            JOIN LATERAL (
                SELECT id, author_id, text, sent_at
                FROM messages
                WHERE dialog_id = d.id
                ORDER BY id DESC
                LIMIT 1
            ) AS m ON TRUE
            JOIN users AS author ON author.id = m.author_id
            WHERE mine.user_id = (SELECT id FROM users WHERE nickname = $1)
            ORDER BY m.id DESC
            """;
        await using NpgsqlCommand command = CreateCommand(connection, sql, user);

        List<DialogSummary> dialogs = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            Message lastMessage = new(
                reader.GetInt64(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetFieldValue<DateTimeOffset>(5));
            dialogs.Add(new DialogSummary(reader.GetInt64(0), reader.GetString(1), lastMessage));
        }

        return dialogs;
    }

    /// <summary>
    ///  Возвращает сообщения диалога с номерами больше afterId, по возрастанию id.
    ///  afterId = 0 означает всю историю. null — пользователь не участник диалога.
    /// </summary>
    public async Task<IReadOnlyList<Message>?> GetMessagesAsync(long dialogId, string user, long afterId)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();

        // История доступна только участникам диалога.
        const string checkSql = """
            SELECT 1
            FROM dialog_participants AS p
            JOIN users AS u ON u.id = p.user_id
            WHERE p.dialog_id = $1 AND u.nickname = $2
            """;
        await using (NpgsqlCommand check = CreateCommand(connection, checkSql, dialogId, user))
        {
            if (await check.ExecuteScalarAsync() is null)
            {
                return null;
            }
        }

        const string sql = """
            SELECT m.id, author.nickname, m.text, m.sent_at
            FROM messages AS m
            JOIN users AS author ON author.id = m.author_id
            WHERE m.dialog_id = $1 AND m.id > $2
            ORDER BY m.id
            """;
        await using NpgsqlCommand command = CreateCommand(connection, sql, dialogId, afterId);

        List<Message> messages = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            messages.Add(ReadMessage(reader));
        }

        return messages;
    }

    /// <summary>
    ///  Сохраняет сообщение и возвращает его с идентификатором диалога. Незнакомые
    ///  пользователи создаются, отсутствующий диалог — создаётся вместе с первым сообщением.
    /// </summary>
    public async Task<SentMessage> SendMessageAsync(string from, string to, string text, DateTimeOffset sentAt)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync();
        await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();

        long fromId = await EnsureUserAsync(connection, transaction, from);
        long toId = from == to ? fromId : await EnsureUserAsync(connection, transaction, to);

        // Advisory lock по паре участников сериализует создание диалога: параллельные «первые»
        // сообщения одной пары выстроятся в очередь, и второй отправитель найдёт уже созданный
        // диалог. Lock держится до конца транзакции.
        string pairKey = $"{long.Min(fromId, toId)}:{long.Max(fromId, toId)}";
        await ExecuteAsync(connection, transaction, "SELECT pg_advisory_xact_lock(hashtext($1))", pairKey);

        long dialogId = await FindDialogAsync(connection, transaction, fromId, toId)
            ?? await CreateDialogAsync(connection, transaction, fromId, toId);

        const string sql = """
            INSERT INTO messages (dialog_id, author_id, text, sent_at)
            VALUES ($1, $2, $3, $4)
            RETURNING id
            """;
        long messageId = await ExecuteScalarLongAsync(connection, transaction, sql, dialogId, fromId, text, sentAt);

        await transaction.CommitAsync();
        return new SentMessage(dialogId, messageId, from, text, sentAt);
    }

    /// <summary>
    ///  Возвращает id пользователя, создав его при первом упоминании.
    /// </summary>
    private static async Task<long> EnsureUserAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string nickname)
    {
        const string insertSql = """
            INSERT INTO users (nickname)
            VALUES ($1)
            ON CONFLICT (nickname) DO NOTHING
            RETURNING id
            """;
        object? inserted = await ExecuteScalarAsync(connection, transaction, insertSql, nickname);
        if (inserted is not null)
        {
            return (long)inserted;
        }

        // Конфликт означает, что пользователь уже есть, — читаем его id.
        return await ExecuteScalarLongAsync(connection, transaction, "SELECT id FROM users WHERE nickname = $1", nickname);
    }

    /// <summary>
    ///  Ищет диалог с ровно таким составом участников (для диалога с самим собой — один участник).
    /// </summary>
    private static async Task<long?> FindDialogAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long fromId, long toId)
    {
        long[] participants = fromId == toId ? [fromId] : [long.Min(fromId, toId), long.Max(fromId, toId)];
        const string sql = """
            SELECT d.id
            FROM dialogs AS d
            JOIN dialog_participants AS p ON p.dialog_id = d.id
            GROUP BY d.id
            HAVING count(*) = cardinality($1) AND bool_and(p.user_id = ANY($1))
            """;
        object? dialogId = await ExecuteScalarAsync(connection, transaction, sql, participants);
        return (long?)dialogId;
    }

    private static async Task<long> CreateDialogAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, long fromId, long toId)
    {
        long dialogId = await ExecuteScalarLongAsync(connection, transaction, "INSERT INTO dialogs DEFAULT VALUES RETURNING id");

        // unnest разворачивает массив участников в строки; для диалога с самим собой строка одна.
        long[] participants = fromId == toId ? [fromId] : [fromId, toId];
        const string sql = """
            INSERT INTO dialog_participants (dialog_id, user_id)
            SELECT $1, user_id FROM unnest($2::bigint[]) AS user_id
            """;
        await ExecuteAsync(connection, transaction, sql, dialogId, participants);

        return dialogId;
    }

    private static Message ReadMessage(NpgsqlDataReader reader)
    {
        return new Message(
            reader.GetInt64(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetFieldValue<DateTimeOffset>(3));
    }

    private static async Task<object?> ExecuteScalarAsync(NpgsqlConnection connection, NpgsqlTransaction? transaction, string sql, params object?[] values)
    {
        await using NpgsqlCommand command = CreateCommand(connection, sql, values);
        command.Transaction = transaction;
        return await command.ExecuteScalarAsync();
    }

    private static async Task<long> ExecuteScalarLongAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, params object?[] values)
    {
        object? value = await ExecuteScalarAsync(connection, transaction, sql, values);
        return value is long id ? id : throw new InvalidOperationException("База не вернула идентификатор.");
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, NpgsqlTransaction transaction, string sql, params object?[] values)
    {
        await using NpgsqlCommand command = CreateCommand(connection, sql, values);
        command.Transaction = transaction;
        await command.ExecuteNonQueryAsync();
    }

    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string sql, params object?[] values)
    {
        NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (object? value in values)
        {
            // Позиционные параметры: значения по порядку соответствуют $1, $2, …
            command.Parameters.AddWithValue(value ?? DBNull.Value);
        }

        return command;
    }
}