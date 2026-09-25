namespace ChatApi;

/// <summary>
///  Запрос на отправку сообщения.
/// </summary>
/// <param name="From">Никнейм отправителя.</param>
/// <param name="To">Никнейм получателя; может совпадать с отправителем.</param>
/// <param name="Text">Текст сообщения.</param>
public sealed record MessageRequest(string From, string To, string Text);