namespace ChatApi.IntegrationTests.Client;

/// <summary> Сообщение в ответах API: история диалога и последнее сообщение. </summary>
/// <param name="Id">Порядковый номер сообщения; растёт с каждым новым сообщением.</param>
/// <param name="Author">Никнейм автора.</param>
/// <param name="Text">Текст сообщения.</param>
/// <param name="SentAt">Время отправки.</param>
internal sealed record ApiMessage(long Id, string Author, string Text, DateTimeOffset SentAt);