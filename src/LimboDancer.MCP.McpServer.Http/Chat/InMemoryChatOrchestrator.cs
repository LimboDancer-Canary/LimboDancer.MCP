using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace LimboDancer.MCP.McpServer.Http.Chat;

public sealed class InMemoryChatOrchestrator : IChatOrchestrator
{
    private readonly ConcurrentDictionary<(string tenantId, string sessionId), SessionState> _sessions = new();
    private readonly ILogger<InMemoryChatOrchestrator> _logger;
    private readonly SemaphoreSlim _globalLock = new(1, 1);

    public InMemoryChatOrchestrator(ILogger<InMemoryChatOrchestrator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> CreateSessionAsync(string tenantId, string? systemPrompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(tenantId));

        var sessionId = Guid.NewGuid().ToString("n");
        var key = (tenantId, sessionId);

        await _globalLock.WaitAsync(ct);
        try
        {
            var state = new SessionState
            {
                Channel = Channel.CreateBounded<ChatEvent>(new BoundedChannelOptions(256)
                {
                    SingleReader = false,
                    SingleWriter = false,
                    FullMode = BoundedChannelFullMode.DropOldest
                }),
                History = new List<(string role, string content)>(),
                Lock = new SemaphoreSlim(1, 1),
                ActiveProcessing = new ConcurrentDictionary<string, CancellationTokenSource>()
            };

            if (!string.IsNullOrWhiteSpace(systemPrompt))
            {
                state.History.Add(("system", systemPrompt!));
            }

            if (!_sessions.TryAdd(key, state))
            {
                throw new InvalidOperationException($"Failed to create session {sessionId}");
            }

            _logger.LogInformation("Created session {SessionId} for tenant {TenantId}", sessionId, tenantId);
            return sessionId;
        }
        finally
        {
            _globalLock.Release();
        }
    }

    public async Task<object> GetHistoryAsync(string tenantId, string sessionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId cannot be null or empty.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("SessionId cannot be null or empty.", nameof(sessionId));

        var key = (tenantId, sessionId);

        if (!_sessions.TryGetValue(key, out var state))
        {
            _logger.LogWarning("Session {SessionId} not found for tenant {TenantId}", sessionId, tenantId);
            return Array.Empty<object>();
        }

        await state.Lock.WaitAsync(ct);
        try
        {
            var result = state.History.Select(x => new { role = x.role, content = x.content }).ToArray();
            return result;
        }
        finally
        {
            state.Lock.Release();
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

        if (!_sessions.TryGetValue(key, out var state))
        {
            throw new InvalidOperationException($"Session {sessionId} not found for tenant {tenantId}.");
        }

        var correlationId = Guid.NewGuid().ToString("n");

        // Add to history with proper locking
        await state.Lock.WaitAsync(ct);
        try
        {
            state.History.Add((role, content));
        }
        finally
        {
            state.Lock.Release();
        }

        // Create cancellation token source for this processing task
        var processingCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        state.ActiveProcessing.TryAdd(correlationId, processingCts);

        // Start processing with proper async handling
        _ = ProcessMessageWithCleanupAsync(key, state, sessionId, content, correlationId, processingCts.Token);

        return correlationId;
    }

    private async Task ProcessMessageWithCleanupAsync(
        (string tenantId, string sessionId) key,
        SessionState state,
        string sessionId,
        string content,
        string correlationId,
        CancellationToken ct)
    {
        try
        {
            await ProcessMessageAsync(state, sessionId, content, correlationId, ct);
        }
        finally
        {
            // Clean up the cancellation token source
            if (state.ActiveProcessing.TryRemove(correlationId, out var cts))
            {
                cts.Dispose();
            }
        }
    }

    private async Task ProcessMessageAsync(
        SessionState state,
        string sessionId,
        string content,
        string correlationId,
        CancellationToken ct)
    {
        try
        {
            // MVP "LLM" stream: echo back tokens with delay
            var text = $"You said: {content}";

            // Use channel writer with cancellation token
            var writer = state.Channel.Writer;

            foreach (var chunk in Chunk(text, 8))
            {
                ct.ThrowIfCancellationRequested();

                if (!await writer.WaitToWriteAsync(ct))
                {
                    _logger.LogWarning("Channel closed while writing token for {CorrelationId}", correlationId);
                    return;
                }

                await writer.WriteAsync(new ChatEvent("token", sessionId, chunk, correlationId), ct);
                await Task.Delay(60, ct);
            }

            // Add assistant response to history
            await state.Lock.WaitAsync(ct);
            try
            {
                state.History.Add(("assistant", text));
            }
            finally
            {
                state.Lock.Release();
            }

            await writer.WriteAsync(new ChatEvent("message.completed", sessionId, text, correlationId), ct);

            _logger.LogDebug("Completed processing message {CorrelationId} for session {SessionId}",
                correlationId, sessionId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Message processing canceled for correlation {CorrelationId}", correlationId);
            // Send cancellation event with best effort
            try
            {
                await state.Channel.Writer.WriteAsync(
                    new ChatEvent("error", sessionId, null, correlationId, "canceled", "Operation was canceled"), ct);
            }
            catch { /* Best effort */ }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {CorrelationId} for session {SessionId}",
                correlationId, sessionId);

            // Send error event with best effort
            try
            {
                await state.Channel.Writer.WriteAsync(
                    new ChatEvent("error", sessionId, null, correlationId, "orchestrator_error", ex.Message), ct);
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

        if (!_sessions.TryGetValue(key, out var state))
        {
            _logger.LogWarning("Attempted to subscribe to non-existent session {SessionId} for tenant {TenantId}",
                sessionId, tenantId);
            yield break;
        }

        // Start heartbeat with proper cancellation
        using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var heartbeatTask = SendHeartbeatAsync(state.Channel, sessionId, heartbeatCts.Token);

        try
        {
            await foreach (var item in state.Channel.Reader.ReadAllAsync(ct))
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
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (!channel.Writer.TryWrite(new ChatEvent("ping", sessionId)))
                {
                    _logger.LogDebug("Failed to write heartbeat for session {SessionId}", sessionId);
                    break;
                }
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

    // Cleanup method to dispose of sessions
    public void Dispose()
    {
        foreach (var (key, state) in _sessions)
        {
            // Cancel all active processing
            foreach (var (_, cts) in state.ActiveProcessing)
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch { }
            }

            // Complete the channel
            state.Channel.Writer.TryComplete();

            // Dispose the lock
            state.Lock.Dispose();
        }

        _sessions.Clear();
        _globalLock.Dispose();
    }

    private sealed class SessionState
    {
        public required Channel<ChatEvent> Channel { get; init; }
        public required List<(string role, string content)> History { get; init; }
        public required SemaphoreSlim Lock { get; init; }
        public required ConcurrentDictionary<string, CancellationTokenSource> ActiveProcessing { get; init; }
    }
}