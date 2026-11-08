namespace ChatApi.IntegrationTests.Client;

/// <summary> Диалог в списке диалогов пользователя. </summary>
/// <param name="DialogId">Идентификатор диалога.</param>
/// <param name="Peer">Никнейм собеседника.</param>
/// <param name="LastMessage">Последнее сообщение диалога.</param>
internal sealed record ApiDialog(long DialogId, string Peer, ApiMessage LastMessage);