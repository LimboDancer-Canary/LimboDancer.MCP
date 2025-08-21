using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace LimboDancer.MCP.McpServer.Http.Chat;

public sealed class InMemoryChatOrchestrator : IChatOrchestrator
{
    private readonly ConcurrentDictionary<(string tenantId, string sessionId), Channel<ChatEvent>> _streams = new();
    private readonly ConcurrentDictionary<(string tenantId, string sessionId), List<(string role, string content)>> _history = new();

    public Task<string> CreateSessionAsync(string tenantId, string? systemPrompt, CancellationToken ct)
    {
        var sessionId = Guid.NewGuid().ToString("n");
        _streams.TryAdd((tenantId, sessionId), Channel.CreateBounded<ChatEvent>(new BoundedChannelOptions(256)
        {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        }));
        _history.TryAdd((tenantId, sessionId), new List<(string role, string content)>());
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            _history[(tenantId, sessionId)].Add(("system", systemPrompt!));
        }
        return Task.FromResult(sessionId);
    }

    public Task<object> GetHistoryAsync(string tenantId, string sessionId, CancellationToken ct)
    {
        _history.TryGetValue((tenantId, sessionId), out var list);
        var result = (list ?? new()).Select(x => new { role = x.role, content = x.content }).ToArray();
        return Task.FromResult<object>(result);
    }

    public async Task<string> EnqueueUserMessageAsync(string tenantId, string sessionId, string role, string content, CancellationToken ct)
    {
        if (!_streams.TryGetValue((tenantId, sessionId), out var channel))
            throw new InvalidOperationException("Session not found.");

        _history[(tenantId, sessionId)].Add((role, content));
        var correlationId = Guid.NewGuid().ToString("n");

        // MVP “LLM” stream: echo back tokens with delay
        _ = Task.Run(async () =>
        {
            try
            {
                var text = $"You said: {content}";
                foreach (var chunk in Chunk(text, 8))
                {
                    await channel.Writer.WriteAsync(new ChatEvent("token", sessionId, chunk, correlationId), ct);
                    await Task.Delay(60, ct);
                }
                await channel.Writer.WriteAsync(new ChatEvent("message.completed", sessionId, text, correlationId), ct);
                _history[(tenantId, sessionId)].Add(("assistant", text));
            }
            catch (OperationCanceledException) { /* ignore */ }
            catch (Exception ex)
            {
                await channel.Writer.WriteAsync(new ChatEvent("error", sessionId, null, correlationId, "orchestrator_error", ex.Message), CancellationToken.None);
            }
        }, CancellationToken.None);

        return correlationId;
    }

    public async IAsyncEnumerable<ChatEvent> SubscribeAsync(string tenantId, string sessionId, [EnumeratorCancellation] CancellationToken ct)
    {
        if (!_streams.TryGetValue((tenantId, sessionId), out var channel))
            yield break;

        // heartbeat
        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await channel.Writer.WriteAsync(new ChatEvent("ping", sessionId), ct);
                    await Task.Delay(TimeSpan.FromSeconds(15), ct);
                }
                catch { break; }
            }
        }, ct);

        while (await channel.Reader.WaitToReadAsync(ct) && channel.Reader.TryRead(out var ev))
        {
            yield return ev;
        }
    }

    private static IEnumerable<string> Chunk(string s, int n)
    {
        for (int i = 0; i < s.Length; i += n)
            yield return s.Substring(i, Math.Min(n, s.Length - i));
    }
}