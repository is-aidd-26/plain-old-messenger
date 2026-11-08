using System.Net;

using ChatApi.Specs.Client;
using ChatApi.Specs.Infrastructure;

using Reqnroll;

namespace ChatApi.Specs.Steps;

/// <summary>
///  Привязки шагов сценариев Gherkin: действия выполняются через публичное HTTP API
///  чата, проверки — по данным из ответов API.
/// </summary>
[Binding]
public sealed class MessagingSteps
{
    private readonly ChatScenarioState _state;

    public MessagingSteps(ChatScenarioState state)
    {
        this._state = state;
    }

    /// <summary> Шаг «Пусть … отправил(а) пользователю … сообщение:» с многострочным текстом. </summary>
    [Given(@"^""(.+)"" отправил(?:а)? пользователю ""(.+)"" (?:первое )?сообщение:$")]
    public async Task SendMessageAsync(string from, string to, string text)
    {
        ApiSentMessage sent = await _state.Client.SendMessageAsync(from, to, text);
        RememberDialog(sent.DialogId, from, to);
    }

    /// <summary> Шаг «Пусть … ответил пользователю … сообщением:» с многострочным текстом. </summary>
    [Given(@"^""(.+)"" ответи(?:л|ла) пользователю ""(.+)"" сообщением:$")]
    public async Task SendReplyAsync(string from, string to, string text)
    {
        ApiSentMessage sent = await _state.Client.SendMessageAsync(from, to, text);
        RememberDialog(sent.DialogId, from, to);
        _state.LastReplyId = sent.Id;
    }

    /// <summary> Шаг «Пусть … отправил(а) себе два сообщения:» с таблицей текстов. </summary>
    [Given(@"^""(.+)"" отправил(?:а)? себе два сообщения:$")]
    public async Task SendSelfMessagesAsync(string user, Table messages)
    {
        foreach (DataTableRow row in messages.Rows)
        {
            ApiSentMessage sent = await _state.Client.SendMessageAsync(user, user, row["Текст"]);
            RememberDialog(sent.DialogId, user, user);
        }
    }

    /// <summary> Шаг «Когда … пытается отправить пользователю … сообщение из одних пробелов». </summary>
    [When(@"^""(.+)"" пытается отправить пользователю ""(.+)"" сообщение из одних пробелов$")]
    public async Task TrySendBlankMessageAsync(string from, string to)
    {
        _state.ErrorResponse = await _state.Client.TrySendMessageAsync(from, to, "   ");
    }

    /// <summary> Шаг «Когда … запрашивает список своих диалогов». </summary>
    [When(@"^""(.+)"" запрашивает список своих диалогов$")]
    public async Task RequestDialogsAsync(string user)
    {
        _state.Dialogs = await _state.Client.GetDialogsAsync(user);
    }

    /// <summary> Шаг «Когда … запрашивает историю этого диалога». </summary>
    [When(@"^""(.+)"" запрашивает историю этого диалога$")]
    public async Task RequestHistoryAsync(string user)
    {
        HttpResponseMessage response = await _state.Client.TryGetMessagesAsync(_state.DialogId, user, after: 0);
        if (response.IsSuccessStatusCode)
        {
            _state.History = await ChatClient.ReadMessagesAsync(response);
            _state.ErrorResponse = null;
        }
        else
        {
            _state.History = [];
            _state.ErrorResponse = response;
        }
    }

    /// <summary> Шаг «Когда … опрашивает новые сообщения после последнего ответа». </summary>
    [When(@"^""(.+)"" опрашивает новые сообщения после последнего ответа$")]
    public async Task PollAfterLastReplyAsync(string user)
    {
        _state.Fresh = await _state.Client.GetMessagesAsync(_state.DialogId, user, _state.LastReplyId);
    }

    /// <summary> Шаг «Когда … отправляет в этот диалог новое сообщение:» с многострочным текстом. </summary>
    [When(@"^""(.+)"" отправляет в этот диалог новое сообщение:$")]
    public async Task SendNewMessageAsync(string from, string text)
    {
        string to = _state.Participants.Single(participant => participant != from);
        _state.LastSent = await _state.Client.SendMessageAsync(from, to, text);
    }

    /// <summary> Шаг «Тогда она увидит ровно один диалог с … и последним сообщением:» с таблицей. </summary>
    [Then(@"^она увидит ровно один диалог с ""(.+)"" и последним сообщением:$")]
    public void DialogListHasSingleDialog(string peer, Table lastMessage)
    {
        ApiDialog dialog = Assert.Single(_state.Dialogs);
        Assert.Equal(_state.DialogId, dialog.DialogId);
        Assert.Equal(peer, dialog.Peer);

        DataTableRow expected = Assert.Single(lastMessage.Rows);
        Assert.Equal(expected["Автор"], dialog.LastMessage.Author);
        Assert.Equal(expected["Текст"], dialog.LastMessage.Text);
    }

    /// <summary> Шаг «Тогда она получит сообщения в порядке отправки:» с таблицей ожидаемых сообщений. </summary>
    [Then("^она получит сообщения в порядке отправки:$")]
    public void HistoryHasMessagesInOrder(Table expected)
    {
        Assert.Equal(expected.Rows.Count, _state.History.Count);
        for (int i = 0; i < _state.History.Count; i++)
        {
            Assert.Equal(expected.Rows[i]["Автор"], _state.History[i].Author);
            Assert.Equal(expected.Rows[i]["Текст"], _state.History[i].Text);
            Assert.InRange(_state.History[i].SentAt, _state.StartedAt, DateTimeOffset.UtcNow);
            if (i > 0)
            {
                Assert.True(_state.History[i - 1].Id < _state.History[i].Id);
            }
        }
    }

    /// <summary> Шаг «Тогда она не получит ни одного сообщения». </summary>
    [Then("^она не получит ни одного сообщения$")]
    public void PollReturnsNoMessages()
    {
        Assert.Empty(_state.Fresh);
    }

    /// <summary> Шаг «Тогда она получит ровно одно новое сообщение:» с таблицей. </summary>
    [Then("^она получит ровно одно новое сообщение:$")]
    public void PollReturnsSingleNewMessage(Table expected)
    {
        ApiMessage message = Assert.Single(_state.Fresh);
        Assert.Equal(_state.LastSent.Id, message.Id);

        DataTableRow row = Assert.Single(expected.Rows);
        Assert.Equal(row["Автор"], message.Author);
        Assert.Equal(row["Текст"], message.Text);
    }

    /// <summary> Шаг «Тогда сервер ответит отказом с кодом NNN и текстом ошибки». </summary>
    [Then(@"^сервер ответит отказом с кодом (\d+) и текстом ошибки$")]
    public async Task ServerRejectsWithErrorAsync(int statusCode)
    {
        Assert.NotNull(_state.ErrorResponse);
        HttpResponseMessage response = _state.ErrorResponse!;
        Assert.Equal((HttpStatusCode)statusCode, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(await ChatClient.ReadErrorAsync(response)));
    }

    /// <summary> Шаг «Тогда у … не будет ни одного диалога». </summary>
    [Then(@"^у ""(.+)"" не будет ни одного диалога$")]
    public async Task UserHasNoDialogsAsync(string user)
    {
        Assert.Empty(await _state.Client.GetDialogsAsync(user));
    }

    private void RememberDialog(long dialogId, string from, string to)
    {
        if (_state.DialogId == 0)
        {
            _state.DialogId = dialogId;
            _state.Participants = [from, to];
        }
    }
}