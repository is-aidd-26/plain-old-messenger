namespace ChatApi;

/// <summary>
///  Диалог не существует или пользователь не участвует в нём. Отдаётся клиенту как 404.
/// </summary>
public sealed class DialogNotFoundException : Exception
{
    public DialogNotFoundException()
    {
    }

    public DialogNotFoundException(string message)
        : base(message)
    {
    }

    public DialogNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}