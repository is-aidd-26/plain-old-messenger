namespace ChatApi;

/// <summary>
///  Диалог в списке диалогов пользователя.
/// </summary>
/// <param name="Peer">Никнейм собеседника.</param>
/// <param name="LastMessage">Последнее сообщение диалога.</param>
public sealed record DialogSummary(string Peer, Message LastMessage);