using System.Net;

using ChatApi.IntegrationTests.Client;
using ChatApi.IntegrationTests.Infrastructure;

using Xunit;

namespace ChatApi.IntegrationTests.Scenarios;

/// <summary>
///  Интеграционные сценарии чата по SRS: проверяют публичное API на настоящем
///  PostgreSQL. Каждый тест получает собственную базу данных в общем контейнере.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class MessagingTests : IAsyncLifetime
{
    private readonly PostgresFixture postgres;
    private string databaseName = default!;
    private ChatApiFactory api = default!;
    private ChatClient chat = default!;

    public MessagingTests(PostgresFixture postgres)
    {
        this.postgres = postgres;
    }

    /// <summary> Создаёт базу теста и поднимает приложение, подключённое к ней. </summary>
    public async Task InitializeAsync()
    {
        databaseName = $"chat_test_{Guid.NewGuid():N}";
        await TestDatabase.CreateAsync(postgres.AdminConnectionString, databaseName);
        api = new ChatApiFactory(TestDatabase.ConnectionString(postgres.AdminConnectionString, databaseName));
        chat = new ChatClient(api.CreateClient());
    }

    /// <summary> Останавливает приложение и удаляет базу теста. </summary>
    public async Task DisposeAsync()
    {
        await api.DisposeAsync();
        await TestDatabase.DropAsync(postgres.AdminConnectionString, databaseName);
    }

    [Fact]
    public async Task FirstConversation_createsDialog_listsIt_andPollsNewMessages()
    {
        DateTimeOffset startedAt = DateTimeOffset.UtcNow;

        // Первое сообщение создаёт пользователей и диалог; ответ собеседника попадает туда же.
        ApiSentMessage first = await chat.SendMessageAsync("alice", "bob", "Привет!");
        ApiSentMessage reply = await chat.SendMessageAsync("bob", "alice", "Здравствуй!");

        Assert.True(first.DialogId > 0);
        Assert.True(first.Id > 0);
        Assert.Equal("alice", first.Author);
        Assert.Equal("Привет!", first.Text);
        Assert.InRange(first.SentAt, startedAt, DateTimeOffset.UtcNow);
        Assert.Equal(first.DialogId, reply.DialogId);
        Assert.True(reply.Id > first.Id);

        // Список диалогов alice: единственный диалог с bob и последним сообщением от него.
        IReadOnlyList<ApiDialog> dialogs = await chat.GetDialogsAsync("alice");
        ApiDialog dialog = Assert.Single(dialogs);
        Assert.Equal(first.DialogId, dialog.DialogId);
        Assert.Equal("bob", dialog.Peer);
        Assert.Equal(reply.Id, dialog.LastMessage.Id);
        Assert.Equal("bob", dialog.LastMessage.Author);
        Assert.Equal("Здравствуй!", dialog.LastMessage.Text);

        // История: оба сообщения по порядку, с автором и временем.
        IReadOnlyList<ApiMessage> history = await chat.GetMessagesAsync(first.DialogId, "alice", after: 0);
        Assert.Equal(2, history.Count);
        Assert.Equal(first.Id, history[0].Id);
        Assert.Equal("alice", history[0].Author);
        Assert.Equal("Привет!", history[0].Text);
        Assert.InRange(history[0].SentAt, startedAt, DateTimeOffset.UtcNow);
        Assert.Equal(reply.Id, history[1].Id);
        Assert.Equal("bob", history[1].Author);
        Assert.Equal("Здравствуй!", history[1].Text);
        Assert.InRange(history[1].SentAt, startedAt, DateTimeOffset.UtcNow);

        // Поллинг: новых сообщений после последнего нет, пока bob не отправит ещё одно.
        Assert.Empty(await chat.GetMessagesAsync(first.DialogId, "alice", after: reply.Id));

        ApiSentMessage extra = await chat.SendMessageAsync("bob", "alice", "Как дела?");
        IReadOnlyList<ApiMessage> fresh = await chat.GetMessagesAsync(first.DialogId, "alice", after: reply.Id);
        ApiMessage polled = Assert.Single(fresh);
        Assert.Equal(extra.Id, polled.Id);
        Assert.Equal("bob", polled.Author);
        Assert.Equal("Как дела?", polled.Text);
    }

    [Fact]
    public async Task SelfDialog_worksLikeRegularDialog()
    {
        ApiSentMessage first = await chat.SendMessageAsync("alice", "alice", "Заметка 1");
        ApiSentMessage second = await chat.SendMessageAsync("alice", "alice", "Заметка 2");
        Assert.Equal(first.DialogId, second.DialogId);
        Assert.True(second.Id > first.Id);

        IReadOnlyList<ApiDialog> dialogs = await chat.GetDialogsAsync("alice");
        ApiDialog dialog = Assert.Single(dialogs);
        Assert.Equal(first.DialogId, dialog.DialogId);
        Assert.Equal("alice", dialog.Peer);
        Assert.Equal(second.Id, dialog.LastMessage.Id);
        Assert.Equal("Заметка 2", dialog.LastMessage.Text);

        IReadOnlyList<ApiMessage> history = await chat.GetMessagesAsync(first.DialogId, "alice", after: 0);
        Assert.Equal(2, history.Count);
        Assert.Equal(("alice", "Заметка 1"), (history[0].Author, history[0].Text));
        Assert.Equal(("alice", "Заметка 2"), (history[1].Author, history[1].Text));
    }

    [Fact]
    public async Task BlankMessage_isRejected_withoutSideEffects()
    {
        HttpResponseMessage response = await chat.TrySendMessageAsync("alice", "bob", "   ");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(string.IsNullOrEmpty(await ChatClient.ReadErrorAsync(response)));

        // Отклонённое сообщение не создаёт ни диалога, ни пользователей.
        Assert.Empty(await chat.GetDialogsAsync("alice"));
    }

    [Fact]
    public async Task DialogHistory_isHidden_fromNonParticipants()
    {
        ApiSentMessage message = await chat.SendMessageAsync("alice", "bob", "Привет!");

        HttpResponseMessage response = await chat.TryGetMessagesAsync(message.DialogId, "carol", after: 0);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        Assert.Empty(await chat.GetDialogsAsync("carol"));
    }
}