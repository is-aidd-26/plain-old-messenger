namespace ChatApi;

/// <summary>
///  Сообщение в диалоге.
/// </summary>
/// <param name="Id">Порядковый номер; растёт с каждым новым сообщением.</param>
/// <param name="Author">Никнейм автора.</param>
/// <param name="Text">Текст сообщения.</param>
/// <param name="SentAt">Время отправки в UTC.</param>
public sealed record Message(long Id, string Author, string Text, DateTimeOffset SentAt);