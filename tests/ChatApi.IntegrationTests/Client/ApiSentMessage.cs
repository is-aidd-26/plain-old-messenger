namespace ChatApi.IntegrationTests.Client;

/// <summary> Ответ API на отправку сообщения. </summary>
/// <param name="DialogId">Идентификатор диалога, в который попало сообщение.</param>
/// <param name="Id">Порядковый номер сообщения.</param>
/// <param name="Author">Никнейм автора.</param>
/// <param name="Text">Текст сообщения.</param>
/// <param name="SentAt">Время отправки.</param>
internal sealed record ApiSentMessage(long DialogId, long Id, string Author, string Text, DateTimeOffset SentAt);