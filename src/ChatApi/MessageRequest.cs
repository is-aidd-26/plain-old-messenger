namespace ChatApi;

/// <summary>
///  Текст сообщения, отправленного в чат.
/// </summary>
/// <param name="Text">Текст сообщения.</param>
public sealed record MessageRequest(string Text);