using FluentAssertions;
using LimboDancer.MCP.Core;
using LimboDancer.MCP.Storage;
using Microsoft.EntityFrameworkCore;

public class ChatHistoryStoreTests
{
    private static ChatHistoryStore MakeSut(string dbName)
    {
        var opts = new DbContextOptionsBuilder<ChatDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        var ctx = new ChatDbContext(opts);
        return new ChatHistoryStore(ctx);
    }

    [Fact]
    public async Task CreateAppendRead_Roundtrip_Works()
    {
        var sut = MakeSut(nameof(CreateAppendRead_Roundtrip_Works));

        var session = await sut.CreateSessionAsync("u1");
        var m1 = await sut.AppendMessageAsync(session.Id, MessageRole.User, "hello");
        var m2 = await sut.AppendMessageAsync(session.Id, MessageRole.Assistant, "hi!");

        var back = await sut.GetMessagesAsync(session.Id, take: 10);
        back.Should().HaveCount(2);
        back[0].Content.Should().Be("hello");
        back[1].Content.Should().Be("hi!");
    }
}