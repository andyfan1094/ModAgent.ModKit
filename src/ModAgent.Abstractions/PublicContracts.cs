using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModAgent.Abstractions;

/// <summary>
/// 标记 Mod DLL 中的公开入口类型。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ModEntryAttribute : Attribute
{
}

/// <summary>
/// 声明可被 SDK 辅助工具或宿主工具发现的 Mod 工具类。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ModToolAttribute(string name, string description) : Attribute
{
    public string Name { get; } = name;
    public string Description { get; } = description;
}

/// <summary>
/// 声明 Mod 需要使用的宿主能力或权限。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ModPermissionAttribute(string permission) : Attribute
{
    public string Permission { get; } = permission;
}

/// <summary>
/// Standard Mod capabilities exposed to the host.
/// </summary>
[Flags]
public enum ModCapability
{
    None = 0,
    Tools = 1 << 0,
    ModelProviders = 1 << 1,
    PipelineHooks = 1 << 2,
    Services = 1 << 3,
    ManagedServices = 1 << 4,
    ConfigPage = 1 << 5,
    ScheduledTasks = 1 << 6
}

/// <summary>
/// Mod 的稳定身份与兼容性信息。
/// </summary>
public interface IModManifest
{
    string Id { get; }
    string Name { get; }
    string Version { get; }
    string Description { get; }
    string Author { get; }
    string HostApiVersionRange { get; }
    IReadOnlyList<string> Dependencies { get; }
    IReadOnlyList<string> Permissions { get; }
    ModCapability Capabilities { get; }
}

/// <summary>
/// Main Mod entry contract. Host implementations call this after loading the DLL.
/// </summary>
public interface IMod
{
    IModManifest Manifest { get; }
    Task InitializeAsync(IModContext context, CancellationToken cancellationToken = default);
    IEnumerable<IModelProvider> GetModelProviders() => Array.Empty<IModelProvider>();
    IEnumerable<ITool> GetTools() => Array.Empty<ITool>();
    IEnumerable<IModService> GetServices() => Array.Empty<IModService>();
    IEnumerable<IManagedService> GetManagedServices() => Array.Empty<IManagedService>();
    IEnumerable<IScheduledTask> GetScheduledTasks() => Array.Empty<IScheduledTask>();
    IEnumerable<IPipelineHook> GetPipelineHooks() => Array.Empty<IPipelineHook>();
    IModConfigPage? GetConfigPage() => null;
}

/// <summary>
/// Mod 访问宿主服务的唯一公开入口。
/// </summary>
public interface IModContext
{
    ILogger Logger { get; }
    IConfigStore Config { get; }
    IDataStore Data { get; }
    IEventBus Events { get; }
    IRuntimeExecutionService Runtime { get; }
    IServiceManager ServiceManager { get; }
    IBrowserHostService? Browser { get; }
    IAuditTrail Audit { get; }
    ISchedulerService? Scheduler { get; }
    string RuntimeDirectory { get; }
    string ModsDirectory { get; }
    string WorkspaceDirectory { get; }
    string DataDirectory { get; }
    string HostVersion { get; }
    CancellationToken HostToken { get; }
    T GetService<T>() where T : class;
}

public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
}

public interface IConfigStore
{
    ValueTask<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);
    ValueTask SetStringAsync(string key, string value, CancellationToken cancellationToken = default);
}

public interface IDataStore
{
    ValueTask<string?> ReadTextAsync(string key, CancellationToken cancellationToken = default);
    ValueTask WriteTextAsync(string key, string value, CancellationToken cancellationToken = default);
    ValueTask<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);
    IAsyncEnumerable<string> ListKeysAsync(string prefix = "", CancellationToken cancellationToken = default);
}

public interface IEventBus
{
    ValueTask PublishAsync(string eventName, JsonElement payload, CancellationToken cancellationToken = default);
    IDisposable Subscribe(string eventName, Func<JsonElement, CancellationToken, ValueTask> handler);
}

public interface IRuntimeExecutionService
{
    Task<RuntimeExecutionResult> ExecuteAsync(RuntimeExecutionRequest request, CancellationToken cancellationToken = default);
}

public sealed record RuntimeExecutionRequest(string Runtime, string Code, IReadOnlyList<string>? Args = null, string? WorkingDirectory = null, int TimeoutSeconds = 30);
public sealed record RuntimeExecutionResult(int ExitCode, string Stdout, string Stderr, bool TimedOut);

public interface IServiceManager
{
    void Register(string name, object service);
}

public interface IBrowserHostService
{
    Task<BrowserPageSnapshot> OpenAsync(string url, CancellationToken cancellationToken = default);
}

public sealed record BrowserPageSnapshot(string Url, string Title, string TextPreview);

public interface IAuditTrail
{
    ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}

public sealed record AuditEvent(string Action, string Summary, DateTimeOffset Timestamp, IReadOnlyDictionary<string, string>? Metadata = null);

public interface ISchedulerService
{
    ValueTask<string> ScheduleAsync(ScheduledTaskDefinition task, CancellationToken cancellationToken = default);
}

public sealed record ScheduledTaskDefinition(string Name, string Cron, string Action);

public interface IModelProvider
{
    string ModelId { get; }
    string DisplayName { get; }
    bool IsAvailable { get; }
    Task<ModelResponse> ChatAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition>? tools, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ModelStreamChunk> StreamAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition>? tools, CancellationToken cancellationToken = default);
}

public sealed record ChatMessage(string Role, string Content, string? Name = null);
public sealed record ModelResponse(string Content, IReadOnlyList<ToolCall>? ToolCalls = null, TokenUsage? Usage = null);
public sealed record ModelStreamChunk(string? ContentDelta = null, IReadOnlyList<ToolCall>? ToolCalls = null, TokenUsage? Usage = null);
public sealed record TokenUsage(int? InputTokens = null, int? OutputTokens = null, int? CachedInputTokens = null);
public sealed record ToolCall(string Id, string Name, string ArgumentsJson);
public sealed record ToolDefinition(string Name, string Description, ToolParametersSchema ParametersSchema);

public interface ITool
{
    string Name { get; }
    string Description { get; }
    ToolParametersSchema ParametersSchema { get; }
    Task<string> ExecuteAsync(string argumentsJson, IToolExecutionContext context, CancellationToken cancellationToken = default);
}

public interface IToolExecutionContext
{
    ILogger Logger { get; }
    string WorkingDirectory { get; }
}

public sealed class ToolParametersSchema
{
    public string Type { get; init; } = "object";
    public Dictionary<string, ToolParameterProperty> Properties { get; init; } = new();
    public List<string> Required { get; init; } = new();
    public bool AdditionalProperties { get; init; }

    public static ToolParametersSchema Empty { get; } = new();
}

public sealed class ToolParameterProperty
{
    public string Type { get; init; } = "string";
    public string? Description { get; init; }
    public IReadOnlyList<string>? Enum { get; init; }
    public JsonElement? Default { get; init; }
}

public interface IModService
{
    string ServiceId { get; }
}

public interface IManagedService
{
    string Name { get; }
    string Description { get; }
    ServiceStatus Status { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ServiceStatus
{
    Stopped,
    Starting,
    Running,
    Degraded,
    Failed
}

public interface IScheduledTask
{
    string Name { get; }
    string Cron { get; }
    Task ExecuteAsync(IModContext context, CancellationToken cancellationToken = default);
}

public interface IModConfigPage
{
    string Title { get; }
    string RenderHtml();
}

public interface IPipelineHook
{
    string Name { get; }
    PipelineHookType Type { get; }
    int Order => 0;
    Task<HookResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default);
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PipelineHookType
{
    BeforeLoop,
    AfterLoop,
    BeforeToolExec,
    AfterToolExec,
    BeforeFileWrite,
    AfterFileWrite,
    BeforeProjectCreate,
    AfterProjectCreate
}

public sealed class HookResult
{
    public bool Continue { get; init; } = true;
    public string? AbortReason { get; init; }
    public string? Summary { get; init; }
    public Dictionary<string, JsonElement> Data { get; init; } = new();

    public static HookResult Ok(string? summary = null) => new() { Summary = summary };
    public static HookResult Abort(string reason) => new() { Continue = false, AbortReason = reason };
}

public sealed class PipelineContext
{
    public string UserMessage { get; set; } = string.Empty;
    public List<ChatMessage> FullHistory { get; } = new();
    public string FinalResponse { get; set; } = string.Empty;
    public List<ToolCallRecord> ToolCallsMade { get; } = new();
    public int TotalRounds { get; set; }
    public long TotalTokensUsed { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public required IModContext ModContext { get; init; }
    public Dictionary<string, object?> Bag { get; } = new();
}

public sealed record ToolCallRecord(string Name, string ArgumentsJson, string? Result, TimeSpan Duration, bool Succeeded);
