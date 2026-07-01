using System.Text.Json;
using ModAgent.Abstractions;
using ModAgent.Mod.Sdk;

namespace HelloToolMod;

[ModEntry]
[ModPermission("tools.execute")]
public sealed class HelloMod : IMod
{
    public IModManifest Manifest { get; } = new ModManifestBuilder
    {
        Id = "samples.hello-tool",
        Name = "问候工具 Mod",
        Version = "1.0.0",
        Description = "最小可运行的 ModAgent 工具 Mod 示例。",
        Author = "ModAgent 贡献者",
        Capabilities = ModCapability.Tools
    }
    .AddPermission("tools.execute")
    .Build();

    public Task InitializeAsync(IModContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.Info("问候工具 Mod 已初始化。");
        return Task.CompletedTask;
    }

    public IEnumerable<ITool> GetTools()
    {
        yield return new HelloTool();
    }
}

[ModTool("hello", "返回一段友好的问候语。")]
public sealed class HelloTool : ITool
{
    public string Name => "hello";
    public string Description => "根据传入姓名返回一段友好的问候语。";

    public ToolParametersSchema ParametersSchema { get; } = new()
    {
        Properties =
        {
            ["name"] = new ToolParameterProperty
            {
                Type = "string",
                Description = "要问候的姓名。"
            }
        }
    };

    public Task<string> ExecuteAsync(string argumentsJson, IToolExecutionContext context, CancellationToken cancellationToken = default)
    {
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson);
        var name = document.RootElement.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString()
            : "开发者";

        return Task.FromResult($"你好，{name}！你的 Mod 已经运行起来了。");
    }
}
