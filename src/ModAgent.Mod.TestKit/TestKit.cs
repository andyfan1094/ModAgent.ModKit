using ModAgent.Abstractions;
using System.Collections.Concurrent;
using System.Text.Json;

namespace ModAgent.Mod.TestKit;

public sealed class TestLogger : ILogger
{
    private readonly List<string> entries = new();

    public IReadOnlyList<string> Entries => entries;

    public void Debug(string message) => entries.Add($"DEBUG {message}");
    public void Info(string message) => entries.Add($"INFO {message}");
    public void Warn(string message) => entries.Add($"WARN {message}");
    public void Error(string message, Exception? exception = null) => entries.Add($"ERROR {message} {exception?.Message}".TrimEnd());
}

public sealed class FakeConfigStore : IConfigStore
{
    private readonly ConcurrentDictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

    public ValueTask<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
    {
        values.TryGetValue(key, out var value);
        return ValueTask.FromResult(value);
    }

    public ValueTask SetStringAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        values[key] = value;
        return ValueTask.CompletedTask;
    }
}

public sealed class FakeDataStore : IDataStore
{
    private readonly ConcurrentDictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);

    public ValueTask<string?> ReadTextAsync(string key, CancellationToken cancellationToken = default)
    {
        values.TryGetValue(key, out var value);
        return ValueTask.FromResult(value);
    }

    public ValueTask WriteTextAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        values[key] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(values.TryRemove(key, out _));
    }

    public async IAsyncEnumerable<string> ListKeysAsync(string prefix = "", [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var key in values.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                yield return key;
            }
        }

        await Task.CompletedTask;
    }
}

public sealed class FakeEventBus : IEventBus
{
    private readonly ConcurrentDictionary<string, List<Func<JsonElement, CancellationToken, ValueTask>>> handlers = new(StringComparer.OrdinalIgnoreCase);

    public ValueTask PublishAsync(string eventName, JsonElement payload, CancellationToken cancellationToken = default)
    {
        if (!handlers.TryGetValue(eventName, out var subscribers))
        {
            return ValueTask.CompletedTask;
        }

        return PublishCoreAsync(subscribers.ToArray(), payload, cancellationToken);
    }

    public IDisposable Subscribe(string eventName, Func<JsonElement, CancellationToken, ValueTask> handler)
    {
        var subscribers = handlers.GetOrAdd(eventName, _ => new List<Func<JsonElement, CancellationToken, ValueTask>>());
        lock (subscribers)
        {
            subscribers.Add(handler);
        }

        return new Subscription(() =>
        {
            lock (subscribers)
            {
                subscribers.Remove(handler);
            }
        });
    }

    private static async ValueTask PublishCoreAsync(IReadOnlyList<Func<JsonElement, CancellationToken, ValueTask>> subscribers, JsonElement payload, CancellationToken cancellationToken)
    {
        foreach (var subscriber in subscribers)
        {
            await subscriber(payload, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class Subscription(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}

public sealed class FakeRuntimeExecutionService : IRuntimeExecutionService
{
    public RuntimeExecutionResult NextResult { get; set; } = new(0, string.Empty, string.Empty, false);

    public Task<RuntimeExecutionResult> ExecuteAsync(RuntimeExecutionRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(NextResult);
    }
}

public sealed class FakeServiceManager : IServiceManager
{
    private readonly Dictionary<string, object> services = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, object> Services => services;

    public void Register(string name, object service)
    {
        services[name] = service;
    }
}

public sealed class FakeAuditTrail : IAuditTrail
{
    private readonly List<AuditEvent> events = new();

    public IReadOnlyList<AuditEvent> Events => events;

    public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        events.Add(auditEvent);
        return ValueTask.CompletedTask;
    }
}

public sealed class TestModContext : IModContext
{
    private readonly Dictionary<Type, object> services = new();

    public TestModContext(string? workspaceDirectory = null)
    {
        WorkspaceDirectory = workspaceDirectory ?? Directory.GetCurrentDirectory();
        RuntimeDirectory = WorkspaceDirectory;
        ModsDirectory = Path.Combine(WorkspaceDirectory, "Mods");
        DataDirectory = Path.Combine(WorkspaceDirectory, "Data");
    }

    public TestLogger TestLogger { get; } = new();
    public FakeConfigStore FakeConfig { get; } = new();
    public FakeDataStore FakeData { get; } = new();
    public FakeEventBus FakeEvents { get; } = new();
    public FakeRuntimeExecutionService FakeRuntime { get; } = new();
    public FakeServiceManager FakeServiceManager { get; } = new();
    public FakeAuditTrail FakeAudit { get; } = new();

    public ILogger Logger => TestLogger;
    public IConfigStore Config => FakeConfig;
    public IDataStore Data => FakeData;
    public IEventBus Events => FakeEvents;
    public IRuntimeExecutionService Runtime => FakeRuntime;
    public IServiceManager ServiceManager => FakeServiceManager;
    public IBrowserHostService? Browser { get; init; }
    public IAuditTrail Audit => FakeAudit;
    public ISchedulerService? Scheduler { get; init; }
    public string RuntimeDirectory { get; }
    public string ModsDirectory { get; }
    public string WorkspaceDirectory { get; }
    public string DataDirectory { get; }
    public string HostVersion { get; init; } = "1.0.0-test";
    public CancellationToken HostToken { get; init; } = CancellationToken.None;

    public TestModContext AddService<T>(T service) where T : class
    {
        services[typeof(T)] = service;
        return this;
    }

    public T GetService<T>() where T : class
    {
        if (services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }

        throw new InvalidOperationException($"未注册 Fake 服务：{typeof(T).FullName}。");
    }
}

public sealed class TestToolExecutionContext(string? workingDirectory = null) : IToolExecutionContext
{
    public TestLogger TestLogger { get; } = new();
    public ILogger Logger => TestLogger;
    public string WorkingDirectory { get; } = workingDirectory ?? Directory.GetCurrentDirectory();
}
