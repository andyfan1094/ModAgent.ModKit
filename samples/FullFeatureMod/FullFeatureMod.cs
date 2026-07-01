using System.Runtime.CompilerServices;
using System.Text.Json;
using ModAgent.Abstractions;
using ModAgent.Mod.Sdk;

namespace FullFeatureMod;

[ModEntry]
[ModPermission("tools.execute")]
[ModPermission("storage.read")]
[ModPermission("storage.write")]
[ModPermission("config.read")]
[ModPermission("events.publish")]
[ModPermission("events.subscribe")]
[ModPermission("runtime.execute")]
[ModPermission("audit.write")]
[ModPermission("pipeline.hooks")]
[ModPermission("models.provide")]
[ModPermission("services.managed")]
[ModPermission("config.page")]
[ModPermission("tasks.schedule")]
public sealed class FullFeatureMod : IMod
{
    private IModContext? modContext;
    private IDisposable? counterSubscription;

    public IModManifest Manifest { get; } = new ModManifestBuilder
    {
        Id = "samples.full-feature",
        Name = "全功能示例 Mod",
        Version = "1.0.0",
        Description = "演示工具、模型提供者、Pipeline Hook、托管服务、定时任务、配置页、事件、运行时、审计、配置和数据存储。",
        Author = "ModAgent 贡献者",
        Capabilities = ModCapability.Tools |
            ModCapability.ModelProviders |
            ModCapability.PipelineHooks |
            ModCapability.ManagedServices |
            ModCapability.ConfigPage |
            ModCapability.ScheduledTasks
    }
    .AddPermission("tools.execute")
    .AddPermission("storage.read")
    .AddPermission("storage.write")
    .AddPermission("config.read")
    .AddPermission("events.publish")
    .AddPermission("events.subscribe")
    .AddPermission("runtime.execute")
    .AddPermission("audit.write")
    .AddPermission("pipeline.hooks")
    .AddPermission("models.provide")
    .AddPermission("services.managed")
    .AddPermission("config.page")
    .AddPermission("tasks.schedule")
    .Build();

    public Task InitializeAsync(IModContext context, CancellationToken cancellationToken = default)
    {
        modContext = context;
        counterSubscription = context.Events.Subscribe("samples.full-feature.counter.changed", async (payload, token) =>
        {
            context.Logger.Info($"收到计数器事件：{payload}");
            await context.Audit.RecordAsync(new AuditEvent(
                "sample.counter.changed",
                "全功能示例收到计数器变化事件。",
                DateTimeOffset.UtcNow,
                new Dictionary<string, string> { ["payload"] = payload.ToString() }), token);
        });

        context.ServiceManager.Register("samples.full-feature.context", context);
        context.Logger.Info("全功能示例 Mod 已初始化。Manifest：" + ModManifestWriter.ToJson(Manifest));
        return Task.CompletedTask;
    }

    public IEnumerable<ITool> GetTools()
    {
        var context = RequireContext();
        return new ITool[]
        {
            new EchoTool(),
            new CounterTool(context),
            new RuntimeTool(context)
        };
    }

    public IEnumerable<IModelProvider> GetModelProviders()
    {
        return new IModelProvider[] { new DemoModelProvider() };
    }

    public IEnumerable<IManagedService> GetManagedServices()
    {
        return new IManagedService[] { new DemoManagedService(RequireContext()) };
    }

    public IEnumerable<IScheduledTask> GetScheduledTasks()
    {
        return new IScheduledTask[] { new DemoScheduledTask() };
    }

    public IEnumerable<IPipelineHook> GetPipelineHooks()
    {
        return new IPipelineHook[]
        {
            new DemoPipelineHook(PipelineHookType.BeforeLoop),
            new DemoPipelineHook(PipelineHookType.AfterLoop)
        };
    }

    public IModConfigPage? GetConfigPage()
    {
        return new DemoConfigPage();
    }

    private IModContext RequireContext()
    {
        return modContext ?? throw new InvalidOperationException("Mod 尚未初始化，无法获取宿主上下文。");
    }

    public void DisposeSubscriptionForTests()
    {
        counterSubscription?.Dispose();
        counterSubscription = null;
    }
}

[ModTool("sample_echo", "回显输入文本，演示普通工具参数解析。")]
public sealed class EchoTool : ITool
{
    public string Name => "sample_echo";
    public string Description => "回显输入文本，演示普通工具参数解析。";
    public ToolParametersSchema ParametersSchema { get; } = new()
    {
        Properties =
        {
            ["text"] = new ToolParameterProperty { Type = "string", Description = "要回显的文本。" }
        },
        Required = { "text" }
    };

    public Task<string> ExecuteAsync(string argumentsJson, IToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(argumentsJson);
        var text = document.RootElement.TryGetProperty("text", out var value) ? value.GetString() : string.Empty;
        context.Logger.Info($"sample_echo 被调用，工作目录：{context.WorkingDirectory}");
        return Task.FromResult(JsonSerializer.Serialize(new { echo = text ?? string.Empty }));
    }
}

[ModTool("sample_counter", "读写宿主数据存储并发布事件。")]
public sealed class CounterTool(IModContext modContext) : ITool
{
    private const string CounterKey = "samples/full-feature/counter";

    public string Name => "sample_counter";
    public string Description => "读写宿主数据存储并发布计数变化事件。";
    public ToolParametersSchema ParametersSchema { get; } = new()
    {
        Properties =
        {
            ["step"] = new ToolParameterProperty { Type = "integer", Description = "本次增加的数量，默认 1。" }
        }
    };

    public async Task<string> ExecuteAsync(string argumentsJson, IToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        var step = 1;
        if (!string.IsNullOrWhiteSpace(argumentsJson))
        {
            using var document = JsonDocument.Parse(argumentsJson);
            if (document.RootElement.TryGetProperty("step", out var value) && value.TryGetInt32(out var parsedStep))
            {
                step = parsedStep;
            }
        }

        var currentText = await modContext.Data.ReadTextAsync(CounterKey, cancellationToken);
        var current = int.TryParse(currentText, out var parsedCurrent) ? parsedCurrent : 0;
        var next = current + step;

        await modContext.Data.WriteTextAsync(CounterKey, next.ToString(), cancellationToken);
        await modContext.Events.PublishAsync(
            "samples.full-feature.counter.changed",
            JsonSerializer.SerializeToElement(new { previous = current, current = next, step }),
            cancellationToken);
        await modContext.Audit.RecordAsync(new AuditEvent(
            "sample.counter.executed",
            $"计数器从 {current} 变为 {next}。",
            DateTimeOffset.UtcNow), cancellationToken);

        return JsonSerializer.Serialize(new { previous = current, current = next, step });
    }
}

[ModTool("sample_runtime_node", "调用宿主运行时执行一段 Node.js 代码。")]
public sealed class RuntimeTool(IModContext modContext) : ITool
{
    public string Name => "sample_runtime_node";
    public string Description => "调用宿主运行时执行一段 Node.js 代码。";
    public ToolParametersSchema ParametersSchema { get; } = new()
    {
        Properties =
        {
            ["code"] = new ToolParameterProperty { Type = "string", Description = "要执行的 JavaScript 代码。" }
        },
        Required = { "code" }
    };

    public async Task<string> ExecuteAsync(string argumentsJson, IToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(argumentsJson);
        var code = document.RootElement.TryGetProperty("code", out var value) ? value.GetString() : string.Empty;
        var result = await modContext.Runtime.ExecuteAsync(new RuntimeExecutionRequest(
            "node",
            code ?? string.Empty,
            WorkingDirectory: context.WorkingDirectory,
            TimeoutSeconds: 10), cancellationToken);

        return JsonSerializer.Serialize(new
        {
            result.ExitCode,
            result.Stdout,
            result.Stderr,
            result.TimedOut
        });
    }
}

public sealed class DemoModelProvider : IModelProvider
{
    public string ModelId => "samples.demo-model.echo";
    public string DisplayName => "示例 Echo 模型";
    public bool IsAvailable => true;

    public Task<ModelResponse> ChatAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition>? tools, CancellationToken cancellationToken = default)
    {
        var lastUserMessage = messages.LastOrDefault(message => message.Role.Equals("user", StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(new ModelResponse($"示例模型收到：{lastUserMessage?.Content ?? "空消息"}"));
    }

    public async IAsyncEnumerable<ModelStreamChunk> StreamAsync(IReadOnlyList<ChatMessage> messages, IReadOnlyList<ToolDefinition>? tools, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await ChatAsync(messages, tools, cancellationToken);
        yield return new ModelStreamChunk(response.Content);
    }
}

public sealed class DemoPipelineHook(PipelineHookType hookType) : IPipelineHook
{
    public string Name => $"samples.full-feature.{hookType}";
    public PipelineHookType Type => hookType;
    public int Order => 100;

    public Task<HookResult> ExecuteAsync(PipelineContext context, CancellationToken cancellationToken = default)
    {
        if (Type == PipelineHookType.BeforeLoop)
        {
            context.Bag["samples.full-feature.startedAt"] = DateTimeOffset.UtcNow;
            context.ModContext.Logger.Debug("BeforeLoop Hook 已执行。");
            return Task.FromResult(HookResult.Ok("全功能示例已记录对话开始时间。"));
        }

        context.ModContext.Logger.Debug($"AfterLoop Hook 已执行，轮数：{context.TotalRounds}");
        return Task.FromResult(HookResult.Ok("全功能示例已记录对话结束信息。"));
    }
}

public sealed class DemoManagedService(IModContext modContext) : IManagedService
{
    public string Name => "samples.full-feature.service";
    public string Description => "演示随宿主启动和停止的托管服务。";
    public ServiceStatus Status { get; private set; } = ServiceStatus.Stopped;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        Status = ServiceStatus.Starting;
        var intervalText = await modContext.Config.GetStringAsync("samples.full-feature.intervalSeconds", cancellationToken);
        modContext.Logger.Info($"示例托管服务启动。配置 intervalSeconds={intervalText ?? "未设置"}");
        Status = ServiceStatus.Running;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        Status = ServiceStatus.Stopped;
        modContext.Logger.Info("示例托管服务停止。");
        return Task.CompletedTask;
    }
}

public sealed class DemoScheduledTask : IScheduledTask
{
    public string Name => "samples.full-feature.heartbeat";
    public string Cron => "*/15 * * * *";

    public async Task ExecuteAsync(IModContext context, CancellationToken cancellationToken = default)
    {
        await context.Events.PublishAsync(
            "samples.full-feature.heartbeat",
            JsonSerializer.SerializeToElement(new { at = DateTimeOffset.UtcNow }),
            cancellationToken);
    }
}

public sealed class DemoConfigPage : IModConfigPage
{
    public string Title => "全功能示例 Mod 配置";

    public string RenderHtml()
    {
        return """
            <section>
              <h1>全功能示例 Mod</h1>
              <p>这里演示 Mod 配置页入口。真实宿主可以把表单值写入 IModContext.Config。</p>
              <label>后台服务间隔秒数 <input name="samples.full-feature.intervalSeconds" value="60" /></label>
            </section>
            """;
    }
}
