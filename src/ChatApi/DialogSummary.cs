namespace ChatApi;

/// <summary>
///  Диалог в списке диалогов пользователя.
/// </summary>
/// <param name="DialogId">Идентификатор диалога; используется для истории и поллинга.</param>
/// <param name="Peer">Никнейм собеседника; для диалога с самим собой — никнейм пользователя.</param>
/// <param name="LastMessage">Последнее сообщение диалога.</param>
public sealed record DialogSummary(long DialogId, string Peer, Message LastMessage);