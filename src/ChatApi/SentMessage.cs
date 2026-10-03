namespace ChatApi;

/// <summary>
///  Сообщение, сохранённое в базе, вместе с идентификатором диалога.
/// </summary>
/// <param name="DialogId">Идентификатор диалога, в который попало сообщение.</param>
/// <param name="Id">Порядковый номер; растёт с каждым новым сообщением.</param>
/// <param name="Author">Никнейм автора.</param>
/// <param name="Text">Текст сообщения.</param>
/// <param name="SentAt">Время отправки в UTC.</param>
public sealed record SentMessage(long DialogId, long Id, string Author, string Text, DateTimeOffset SentAt);