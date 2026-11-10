using ChatApi.Specs.Client;

namespace ChatApi.Specs.Infrastructure;

/// <summary>
///  Данные текущего сценария: клиент API, диалог и последние ответы сервера,
///  которые проверяются шагами «Тогда». Экземпляр создаётся Reqnroll на каждый
///  сценарий и внедряется в привязки шагов и хуки.
/// </summary>
public sealed class ChatScenarioState
{
    /// <summary> Клиент API; поднимается хуком BeforeScenario. </summary>
    public ChatClient Client { get; set; } = default!;

    /// <summary> Момент начала сценария; используется для проверки времени сообщений. </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary> Идентификатор диалога сценария; 0 — диалог ещё не создан. </summary>
    public long DialogId { get; set; }

    /// <summary> Участники диалога сценария. </summary>
    public IReadOnlyList<string> Participants { get; set; } = [];

    /// <summary> Идентификатор последнего ответа собеседника; от него отсчитывается опрос. </summary>
    public long LastReplyId { get; set; }

    /// <summary> Сообщение, отправленное последним шагом действия. </summary>
    public ApiSentMessage LastSent { get; set; } = default!;

    /// <summary> Последний полученный список диалогов. </summary>
    public IReadOnlyList<ApiDialog> Dialogs { get; set; } = [];

    /// <summary> Последняя полученная история диалога. </summary>
    public IReadOnlyList<ApiMessage> History { get; set; } = [];

    /// <summary> Последний результат опроса новых сообщений. </summary>
    public IReadOnlyList<ApiMessage> Fresh { get; set; } = [];

    /// <summary> Последний неуспешный ответ API, если он был. </summary>
    public HttpResponseMessage? ErrorResponse { get; set; }
}