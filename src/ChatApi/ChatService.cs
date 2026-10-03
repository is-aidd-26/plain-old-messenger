namespace ChatApi;

/// <summary>
///  Фасад над хранилищем: проверяет входные значения, держит бизнес-правила
///  (лимиты, диалог с самим собой) и логирует отправку. SQL живёт в PostgresChatStore.
/// </summary>
public sealed class ChatService(PostgresChatStore store, ILogger<ChatService> logger)
{
    private const int MaxNicknameLength = 64;
    private const int MaxTextLength = 500;

    /// <summary>
    ///  Возвращает список диалогов пользователя.
    /// </summary>
    public Task<IReadOnlyList<DialogSummary>> GetDialogsAsync(string user)
    {
        return store.GetDialogsAsync(ValidateNickname(user, "пользователя"));
    }

    /// <summary>
    ///  Возвращает сообщения диалога с номерами больше afterId. afterId = 0 означает всю историю.
    /// </summary>
    public async Task<IReadOnlyList<Message>> GetMessagesAsync(long dialogId, string user, long afterId)
    {
        if (dialogId < 1)
        {
            throw new ArgumentException("Некорректный идентификатор диалога.");
        }

        if (afterId < 0)
        {
            throw new ArgumentException("Параметр after должен быть неотрицательным числом.");
        }

        IReadOnlyList<Message>? messages = await store.GetMessagesAsync(dialogId, ValidateNickname(user, "пользователя"), afterId);
        return messages ?? throw new DialogNotFoundException($"Диалог {dialogId} не найден.");
    }

    /// <summary>
    ///  Отправляет сообщение от from к to и возвращает сохранённое сообщение
    ///  вместе с идентификатором диалога. Незнакомые пользователи создаются лениво.
    /// </summary>
    public async Task<SentMessage> SendMessageAsync(string from, string to, string text)
    {
        string author = ValidateNickname(from, "отправителя");
        string peer = ValidateNickname(to, "получателя");
        string trimmedText = ValidateText(text);

        SentMessage message = await store.SendMessageAsync(author, peer, trimmedText, DateTimeOffset.UtcNow);
        logger.LogInformation("Сообщение от {From} к {To}: {Text}", author, peer, message.Text);
        return message;
    }

    /// <summary>
    ///  Обрезает никнейм и проверяет длину; возвращает обрезанное значение.
    /// </summary>
    private static string ValidateNickname(string? nickname, string role)
    {
        if (string.IsNullOrWhiteSpace(nickname))
        {
            throw new ArgumentException($"Никнейм {role} не указан.");
        }

        string trimmed = nickname.Trim();
        if (trimmed.Length > MaxNicknameLength)
        {
            throw new ArgumentException($"Никнейм {role} длиннее {MaxNicknameLength} символов.");
        }

        return trimmed;
    }

    /// <summary>
    ///  Обрезает текст и проверяет длину; возвращает обрезанное значение.
    /// </summary>
    private static string ValidateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Сообщение не может быть пустым.");
        }

        string trimmed = text.Trim();
        if (trimmed.Length > MaxTextLength)
        {
            throw new ArgumentException($"Сообщение длиннее {MaxTextLength} символов.");
        }

        return trimmed;
    }
}