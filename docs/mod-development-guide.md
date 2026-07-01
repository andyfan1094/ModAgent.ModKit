# Mod 开发完整指南

本文面向第三方 Mod 开发者，说明如何在核心闭源的情况下开发、测试、调试和发布自己的 Mod。

## 1. 开发前准备

开发者只需要安装公开工具：

- .NET SDK 10 或更高版本。
- 一个支持断点调试的 IDE，例如 Rider、Visual Studio、VS Code。
- ModAgent DevHost 或正式 App 的开发模式二进制。
- 本仓库中的 `ModAgent.Abstractions`、`ModAgent.Mod.Sdk`、`ModAgent.Mod.TestKit`。

开发者不需要，也不应该引用闭源核心项目。

## 2. 创建 Mod 项目

一个最小 Mod 项目通常只需要引用公开接口包：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="ModAgent.Abstractions" Version="1.0.0" />
    <PackageReference Include="ModAgent.Mod.Sdk" Version="1.0.0" PrivateAssets="all" />
    <PackageReference Include="ModAgent.Mod.TestKit" Version="1.0.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

本仓库本地开发时也可以使用 `ProjectReference`，示例项目就是这种方式。

## 3. Mod 入口

每个 Mod DLL 必须提供一个入口类：

- 使用 `[ModEntry]` 标记。
- 实现 `IMod`。
- 提供 `IModManifest`。
- 在 `InitializeAsync` 中完成初始化。
- 按需返回工具、模型提供者、Hook、托管服务或事件订阅器。

示例：

```csharp
[ModEntry]
public sealed class MyMod : IMod
{
    public IModManifest Manifest { get; } = new ModManifestBuilder
    {
        Id = "example.my-mod",
        Name = "我的 Mod",
        Version = "1.0.0",
        Description = "示例 Mod。",
        Author = "开发者",
        Capabilities = ModCapability.Tools
    }.Build();

    public Task InitializeAsync(IModContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.Info("Mod 已初始化。");
        return Task.CompletedTask;
    }
}
```

## 4. Manifest 字段

`IModManifest` 是宿主加载 Mod 的核心元信息：

| 字段 | 说明 |
| --- | --- |
| `Id` | 全局唯一 ID，建议使用反向域名或命名空间风格。 |
| `Name` | 展示名称。 |
| `Version` | Mod 版本，建议使用语义化版本。 |
| `Description` | 简短说明。 |
| `Author` | 作者或组织。 |
| `HostApiVersionRange` | 兼容的宿主 API 版本范围。 |
| `Dependencies` | 依赖的其他 Mod ID。 |
| `Permissions` | 需要的权限。 |
| `Capabilities` | 提供的能力集合。 |

## 5. 能力类型

公开接口当前支持这些能力：

| 能力 | 接口 | 用途 |
| --- | --- | --- |
| 工具 | `ITool` | 给 AI 暴露可调用工具。 |
| 模型提供者 | `IModelProvider` | 接入自定义模型或代理模型。 |
| Pipeline Hook | `IPipelineHook` | 在对话循环前后注入逻辑。 |
| 托管服务 | `IHostedService` | 随宿主启动和停止后台服务。 |
| 事件订阅 | `IEventSubscriber` | 订阅宿主或其他 Mod 发布的事件。 |

完整写法见 `samples/FullFeatureMod/FullFeatureMod.cs`。

## 6. 工具开发

工具需要实现 `ITool`：

- `Name`：工具名，建议使用小写加下划线。
- `Description`：给模型看的说明，必须清晰。
- `ParametersSchema`：参数 JSON Schema 的简化描述。
- `ExecuteAsync`：执行工具逻辑。

工具入参是 JSON 字符串，返回值也是字符串。复杂结果建议返回 JSON 字符串。

## 7. 使用宿主能力

Mod 不能直接引用核心内部类，只能通过 `IModContext` 使用宿主能力：

| 能力 | 入口 |
| --- | --- |
| 日志 | `context.Logger` |
| 配置 | `context.Config` |
| 数据存储 | `context.DataStore` |
| 事件总线 | `context.EventBus` |
| 服务获取 | `context.GetService<T>()` |
| 运行目录 | `RuntimeDirectory`、`WorkspaceDirectory`、`DataDirectory` |
| 宿主版本 | `HostVersion` |
| 退出信号 | `HostToken` |

工具执行上下文 `IToolExecutionContext` 也可以通过 `GetService<T>()` 获取宿主注入的服务。示例中 `sample_counter` 通过 `IModContext` 读写数据存储。

## 8. 权限声明

如果 Mod 要访问敏感能力，必须声明权限：

```csharp
[ModPermission("storage.write")]
[ModPermission("events.publish")]
public sealed class MyMod : IMod
{
}
```

建议权限粒度保持清晰：

- `tools.execute`
- `storage.read`
- `storage.write`
- `config.read`
- `events.publish`
- `events.subscribe`
- `pipeline.hooks`
- `models.provide`
- `services.hosted`
- `network.access`
- `filesystem.read`
- `filesystem.write`

最终权限是否允许由闭源宿主决定。

## 9. 本地测试

使用 `ModAgent.Mod.TestKit` 可以不启动真实宿主也测试 Mod：

```csharp
var context = new TestModContext();
var mod = new MyMod();
await mod.InitializeAsync(context);

var tool = mod.GetTools().Single(tool => tool.Name == "hello");
var toolContext = new TestToolExecutionContext();
var result = await tool.ExecuteAsync("{\"name\":\"开发者\"}", toolContext);
```

TestKit 提供：

- `TestLogger`
- `FakeConfigStore`
- `FakeDataStore`
- `FakeEventBus`
- `TestModContext`
- `TestToolExecutionContext`

## 10. 使用 DevHost 调试

DevHost 是闭源核心的可运行二进制，只用于加载和调试开发者自己的 Mod：

```bash
modagent dev --mod ./bin/Debug/net10.0/MyMod.dll --wait-for-debugger
```

IDE 配置示例：

```json
{
  "executablePath": "/Applications/ModAgent.app/Contents/MacOS/ModAgent",
  "commandLineArgs": "--dev --mod $(TargetPath) --wait-for-debugger"
}
```

调试时断点打在开发者自己的 Mod 项目里。宿主源码不会公开，也不会参与调试。

## 11. 打包发布

推荐发布内容：

```text
MyMod/
  MyMod.dll
  MyMod.deps.json
  MyMod.runtimeconfig.json
  mod.manifest.json
  libs/
    第三方依赖.dll
```

可以使用 `ModManifestWriter.ToJson(mod.Manifest)` 生成 Manifest JSON，也可以在 CI 中校验 Manifest。

## 12. 兼容性建议

- `ModAgent.Abstractions` 使用语义化版本。
- Mod 尽量只依赖公开接口，不使用反射访问宿主内部类型。
- `HostApiVersionRange` 写清楚兼容范围，例如 `>=1.0.0 <2.0.0`。
- 破坏性变更只在 Abstractions 主版本升级时发生。
- Mod 应该优雅处理权限拒绝、配置缺失和服务不可用。

## 13. 安全建议

- 不在日志中输出 Token、密钥、Cookie、私有路径或用户敏感数据。
- 对文件、网络、命令行等能力做最小权限申请。
- 工具执行前校验参数。
- 长耗时任务支持 `CancellationToken`。
- 后台服务停止时释放资源。

## 14. 示例清单

- `samples/HelloToolMod`：最小工具 Mod。
- `samples/FullFeatureMod`：演示当前公开接口的全部主要能力。
