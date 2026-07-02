using System.Reflection;
using System.Text.Json;
using ModAgent.Abstractions;
using ModAgent.Mod.TestKit;

PrintBanner();

// ---------- 解析参数 ----------
var modPath = args.FirstOrDefault(a => a.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
var workingDir = args.FirstOrDefault(a => a.StartsWith("--workdir="))?["--workdir=".Length..] ?? Directory.GetCurrentDirectory();
var toolName = args.FirstOrDefault(a => a.StartsWith("--tool="))?["--tool=".Length..];
var toolArgs = args.FirstOrDefault(a => a.StartsWith("--args="))?["--args=".Length..];
var listOnly = args.Contains("--list");
var nonInteractive = args.Contains("--non-interactive");
var verbose = args.Contains("--verbose");

if (modPath is null)
{
    Console.Error.WriteLine("用法: modagent-testhost <Mod.dll> [--workdir=<dir>] [--tool=<tool> --args=<json>] [--list] [--non-interactive] [--verbose]");
    return 1;
}

if (!File.Exists(modPath))
{
    Console.Error.WriteLine($"错误: 找不到文件 {modPath}");
    return 1;
}

// ---------- 加载 Mod DLL ----------
Console.WriteLine($"📦 加载 Mod: {modPath}");
var assembly = Assembly.LoadFrom(Path.GetFullPath(modPath));

// 查找 [ModEntry] 类型
var modType = assembly.GetExportedTypes()
    .FirstOrDefault(t => t.GetCustomAttribute<ModEntryAttribute>() is not null
                         && typeof(IMod).IsAssignableFrom(t));

if (modType is null)
{
    Console.Error.WriteLine("错误: DLL 中找不到带 [ModEntry] 且实现 IMod 的类型。");
    return 1;
}

Console.WriteLine($"🔍 找到 Mod 入口: {modType.FullName}");

// ---------- 初始化 Mod ----------
var mod = (IMod)Activator.CreateInstance(modType)!;
Console.WriteLine($"📋 Mod: {mod.Manifest.Name} v{mod.Manifest.Version} ({mod.Manifest.Id})");
Console.WriteLine($"   作者: {mod.Manifest.Author}");
Console.WriteLine($"   能力: {mod.Manifest.Capabilities}");
Console.WriteLine($"   权限: {string.Join(", ", mod.Manifest.Permissions)}");
Console.WriteLine();

var context = new TestModContext(workingDir);
await mod.InitializeAsync(context, CancellationToken.None);

// 收集工具
var tools = mod.GetTools().ToList();
Console.WriteLine($"🔧 工具数: {tools.Count}");
foreach (var tool in tools)
{
    Console.WriteLine($"   - {tool.Name}: {tool.Description}");
}
Console.WriteLine();

if (listOnly)
{
    return 0;
}

// ---------- 交互模式 / 单次执行 ----------
if (nonInteractive || toolName is not null)
{
    var target = toolName ?? tools.FirstOrDefault()?.Name;
    if (target is null)
    {
        Console.Error.WriteLine("错误: 该 Mod 没有注册任何工具。");
        return 1;
    }

    var tool = tools.FirstOrDefault(t => t.Name == target);
    if (tool is null)
    {
        Console.Error.WriteLine($"错误: 找不到工具 {target}。可用: {string.Join(", ", tools.Select(t => t.Name))}");
        return 1;
    }

    await RunTool(tool, toolArgs ?? "{}", context);
    return 0;
}

// 交互模式
Console.WriteLine("💬 交互模式。输入 'list' 查看工具，'run <工具名> <json>' 调用工具，'quit' 退出。");
while (true)
{
    Console.Write("> ");
    var line = Console.ReadLine();
    if (line is null || line.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
        break;

    if (line.Trim().Equals("list", StringComparison.OrdinalIgnoreCase))
    {
        foreach (var t in tools)
            Console.WriteLine($"  {t.Name}: {t.Description}");
        continue;
    }

    if (line.StartsWith("run ", StringComparison.OrdinalIgnoreCase))
    {
        var parts = line[4..].Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var name = parts[0];
        var json = parts.Length > 1 ? parts[1] : "{}";
        var tool = tools.FirstOrDefault(t => t.Name == name);
        if (tool is null)
        {
            Console.WriteLine($"未知工具: {name}");
            continue;
        }
        await RunTool(tool, json, context);
        continue;
    }

    Console.WriteLine("未知命令。输入 'list', 'run <tool> <json>', 'quit'。");
}

Console.WriteLine("👋 再见！");
return 0;

// ---------- helper ----------
async Task RunTool(ITool tool, string argsJson, TestModContext ctx)
{
    var execCtx = new TestToolExecutionContext(workingDir);
    Console.WriteLine($"⚡ 调用 {tool.Name}({argsJson})");
    try
    {
        var result = await tool.ExecuteAsync(argsJson, execCtx, CancellationToken.None);
        Console.WriteLine($"✅ 结果: {result}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"❌ 失败: {ex.Message}");
        if (verbose) Console.Error.WriteLine(ex);
    }
}

static void PrintBanner()
{
    Console.WriteLine("""
    ╔══════════════════════════════════════╗
    ║     ModAgent Mod TestHost            ║
    ║     本地 Mod 交互式测试工具          ║
    ╚══════════════════════════════════════╝
    """);
}
