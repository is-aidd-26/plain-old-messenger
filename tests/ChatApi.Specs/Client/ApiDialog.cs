namespace ChatApi.Specs.Client;

/// <summary> Диалог в списке диалогов пользователя. </summary>
/// <param name="DialogId">Идентификатор диалога.</param>
/// <param name="Peer">Никнейм собеседника.</param>
/// <param name="LastMessage">Последнее сообщение диалога.</param>
public sealed record ApiDialog(long DialogId, string Peer, ApiMessage LastMessage);