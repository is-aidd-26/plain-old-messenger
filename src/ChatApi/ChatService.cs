namespace ChatApi;

/// <summary>
///  Хранит диалоги в памяти процесса. Диалог — пара никнеймов (пользователь, собеседник);
///  каждое сообщение попадает в обе стороны диалога, поэтому у каждого участника своя полная история.
///  После перезапуска процесса все данные сбрасываются.
/// </summary>
public sealed class ChatService(ILogger<ChatService> logger)
{
    private const int MaxTextLength = 500;

    // Сервис зарегистрирован как singleton, поэтому доступ из параллельных запросов защищаем блокировкой.
    private readonly object gate = new();
    private readonly Dictionary<string, Dictionary<string, List<Message>>> dialogs = new();
    private long lastMessageId;

    /// <summary>
    ///  Возвращает список диалогов пользователя.
    /// </summary>
    public IReadOnlyList<DialogSummary> GetDialogs(string user)
    {
        ValidateNickname(user, "пользователя");

        lock (gate)
        {
            if (!dialogs.TryGetValue(user, out Dictionary<string, List<Message>>? byPeer))
            {
                return [];
            }

            return byPeer.Select(pair => new DialogSummary(pair.Key, pair.Value[^1])).ToList();
        }
    }

    /// <summary>
    ///  Возвращает сообщения диалога с номерами больше afterId. afterId = 0 означает всю историю.
    /// </summary>
    public IReadOnlyList<Message> GetMessages(string user, string peer, long afterId)
    {
        ValidateNickname(user, "пользователя");
        ValidateNickname(peer, "собеседника");

        lock (gate)
        {
            if (!dialogs.TryGetValue(user, out Dictionary<string, List<Message>>? byPeer)
                || !byPeer.TryGetValue(peer, out List<Message>? messages))
            {
                return [];
            }

            return messages.Where(message => message.Id > afterId).ToList();
        }
    }

    /// <summary>
    ///  Отправляет сообщение от from к to и возвращает сохранённое сообщение.
    /// </summary>
    public Message SendMessage(string from, string to, string text)
    {
        ValidateNickname(from, "отправителя");
        ValidateNickname(to, "получателя");

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Сообщение не может быть пустым.");
        }

        if (text.Length > MaxTextLength)
        {
            throw new ArgumentException($"Сообщение длиннее {MaxTextLength} символов.");
        }

        Message message;
        lock (gate)
        {
            message = new Message(++lastMessageId, from, text.Trim(), DateTimeOffset.UtcNow);
            Append(from, to, message);

            // Диалог с самим собой уже создан вызовом выше — не дублируем сообщение.
            if (from != to)
            {
                Append(to, from, message);
            }
        }

        logger.LogInformation("Сообщение от {From} к {To}: {Text}", from, to, message.Text);
        return message;
    }

    private void Append(string user, string peer, Message message)
    {
        if (!dialogs.TryGetValue(user, out Dictionary<string, List<Message>>? byPeer))
        {
            byPeer = new Dictionary<string, List<Message>>();
            dialogs[user] = byPeer;
        }

        if (!byPeer.TryGetValue(peer, out List<Message>? messages))
        {
            messages = new List<Message>();
            byPeer[peer] = messages;
        }

        messages.Add(message);
    }

    private static void ValidateNickname(string nickname, string role)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            throw new ArgumentException($"Никнейм {role} не указан.");
        }
    }
}