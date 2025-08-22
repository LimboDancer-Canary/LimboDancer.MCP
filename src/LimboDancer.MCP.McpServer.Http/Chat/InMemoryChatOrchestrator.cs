using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Http.Chat;

public sealed class InMemoryChatOrchestrator : IChatOrchestrator
{
    private readonly ConcurrentDictionary<(string tenantId, string sessionId), Channel<ChatEvent>> _streams = new();
    private readonly ConcurrentDictionary<(string tenantId, string sessionId), List<(string role, string content)>> _history = new();
    private readonly ConcurrentDictionary<(string tenantId, string sessionId), SemaphoreSlim> _sessionLocks = new();
    private readonly ILogger<InMemoryChatOrchestrator> _logger;

    public InMemoryChatOrchestrator(ILogger<InMemoryChatOrchestrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> CreateSessionAsync(string tenantId, string? systemPrompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(tenantId));

        var sessionId = Guid.NewGuid().ToString("n");
        var key = (tenantId, sessionId);

        // Create channel with proper error handling
        var channel = Channel.CreateBounded<ChatEvent>(new BoundedChannelOptions(256)
        {
            SingleReader = false,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

        if (!_streams.TryAdd(key, channel))
        {
            throw new InvalidOperationException($"Failed to create session {sessionId}");
        }

        var historyList = new List<(string role, string content)>();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            historyList.Add(("system", systemPrompt!));
        }

        if (!_history.TryAdd(key, historyList))
        {
            // Rollback channel creation
            _streams.TryRemove(key, out _);
            throw new InvalidOperationException($"Failed to initialize session history for {sessionId}");
        }

        _sessionLocks.TryAdd(key, new SemaphoreSlim(1, 1));

        _logger.LogInformation("Created session {SessionId} for tenant {TenantId}", sessionId, tenantId);
        return Task.FromResult(sessionId);
    }

    public async Task<object> GetHistoryAsync(string tenantId, string sessionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId cannot be null or empty.", nameof(sessionId));

        var key = (tenantId, sessionId);

        if (!_sessionLocks.TryGetValue(key, out var sessionLock))
        {
            _logger.LogWarning("Session {SessionId} not found for tenant {TenantId}", sessionId, tenantId);
            return Array.Empty<object>();
        }

        await sessionLock.WaitAsync(ct);
        try
        {
            if (_history.TryGetValue(key, out var list))
            {
                var result = list.Select(x => new { role = x.role, content = x.content }).ToArray();
                return result;
            }

            return Array.Empty<object>();
        }
        finally
        {
            sessionLock.Release();
        }
    }

    public async Task<string> EnqueueUserMessageAsync(string tenantId, string sessionId, string role, string content, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId cannot be null or empty.", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content cannot be null or empty.", nameof(content));

        var key = (tenantId, sessionId);

        if (!_streams.TryGetValue(key, out var channel))
        {
            throw new InvalidOperationException($"Session {sessionId} not found for tenant {tenantId}.");
        }

        if (!_sessionLocks.TryGetValue(key, out var sessionLock))
        {
            throw new InvalidOperationException($"Session lock not found for {sessionId}.");
        }

        var correlationId = Guid.NewGuid().ToString("n");

        // Add to history with proper locking
        await sessionLock.WaitAsync(ct);
        try
        {
            if (_history.TryGetValue(key, out var list))
            {
                list.Add((role, content));
            }
        }
        finally
        {
            sessionLock.Release();
        }

        // Start async processing with proper error handling
        _ = ProcessMessageAsync(key, sessionId, content, correlationId, channel, sessionLock, ct);

        return correlationId;
    }

    private async Task ProcessMessageAsync(
        (string tenantId, string sessionId) key,
        string sessionId,
        string content,
        string correlationId,
        Channel<ChatEvent> channel,
        SemaphoreSlim sessionLock,
        CancellationToken ct)
    {
        try
        {
            // MVP "LLM" stream: echo back tokens with delay
            var text = $"You said: {content}";

            foreach (var chunk in Chunk(text, 8))
            {
                await channel.Writer.WriteAsync(new ChatEvent("token", sessionId, chunk, correlationId), ct);
                await Task.Delay(60, ct);
            }

            // Add assistant response to history
            await sessionLock.WaitAsync(ct);
            try
            {
                if (_history.TryGetValue(key, out var list))
                {
                    list.Add(("assistant", text));
                }
            }
            finally
            {
                sessionLock.Release();
            }

            await channel.Writer.WriteAsync(new ChatEvent("message.completed", sessionId, text, correlationId), ct);

            _logger.LogDebug("Completed processing message {CorrelationId} for session {SessionId}",
                correlationId, sessionId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message processing canceled for correlation {CorrelationId}", correlationId);
            // Send cancellation event
            try
            {
                await channel.Writer.WriteAsync(
                    new ChatEvent("error", sessionId, null, correlationId, "canceled", "Operation was canceled"),
                    CancellationToken.None);
            }
            catch { /* Best effort */ }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {CorrelationId} for session {SessionId}",
                correlationId, sessionId);

            // Send error event
            try
            {
                await channel.Writer.WriteAsync(
                    new ChatEvent("error", sessionId, null, correlationId, "orchestrator_error", ex.Message),
                    CancellationToken.None);
            }
            catch (Exception writeEx)
            {
                _logger.LogError(writeEx, "Failed to write error event for correlation {CorrelationId}", correlationId);
            }
        }
    }

    public async IAsyncEnumerable<ChatEvent> SubscribeAsync(
        string tenantId,
        string sessionId,
        [EnumeratorCancellation] CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId cannot be null or empty.", nameof(sessionId));

        var key = (tenantId, sessionId);

        if (!_streams.TryGetValue(key, out var channel))
        {
            _logger.LogWarning("Attempted to subscribe to non-existent session {SessionId} for tenant {TenantId}",
                sessionId, tenantId);
            yield break;
        }

        // Start heartbeat with proper cancellation
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeatTask = SendHeartbeatAsync(channel, sessionId, heartbeatCts.Token);

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(ct))
            {
                yield return item;
            }
        }
        finally
        {
            heartbeatCts.Cancel();
            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

    private async Task SendHeartbeatAsync(Channel<ChatEvent> channel, string sessionId, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await channel.Writer.WriteAsync(new ChatEvent("ping", sessionId), ct);
                await Task.Delay(TimeSpan.FromSeconds(15), ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on cancellation
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Heartbeat task failed for session {SessionId}", sessionId);
        }
    }

    private static IEnumerable<string> Chunk(string s, int n)
    {
        if (string.IsNullOrEmpty(s)) yield break;

        for (int i = 0; i < s.Length; i += n)
            yield return s.Substring(i, Math.Min(n, s.Length - i));
    }
}