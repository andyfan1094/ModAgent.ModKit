# 全功能示例 Mod 说明

`FullFeatureMod` 用一个示例展示 ModAgent 公开接口的主要能力。它不是业务 Mod，而是给开发者复制、改造和学习的参考实现。

## 示例位置

```text
samples/FullFeatureMod/
  FullFeatureMod.csproj
  FullFeatureMod.cs
```

## 覆盖能力

| 能力 | 示例类型 | 说明 |
| --- | --- | --- |
| Mod 入口 | `FullFeatureMod` | 使用 `[ModEntry]` 并实现 `IMod`。 |
| Manifest | `ModManifestBuilder` | 声明 ID、名称、版本、权限和能力。 |
| 权限 | `[ModPermission]` | 演示工具、存储、事件、Hook、模型等权限声明。 |
| 工具 | `EchoTool` | 演示参数 Schema 和普通工具执行。 |
| 数据存储 | `CounterTool` | 使用宿主 `DataStore` 保存计数器。 |
| 事件发布 | `CounterTool` | 计数变化后发布事件。 |
| 模型提供者 | `DemoModelProvider` | 暴露一个 Echo 风格的示例模型。 |
| Pipeline Hook | `DemoPipelineHook` | 在对话循环前后记录状态。 |
| 托管服务 | `DemoHostedService` | 演示随宿主启动的后台服务。 |
| 事件订阅 | `DemoEventSubscriber` | 订阅并处理计数器变化事件。 |

## 构建

在仓库根目录执行：

```bash
dotnet build samples/FullFeatureMod/FullFeatureMod.csproj --configuration Release
```

输出 DLL 通常位于：

```text
samples/FullFeatureMod/bin/Release/net10.0/FullFeatureMod.dll
```

## 使用 DevHost 加载

```bash
modagent dev --mod samples/FullFeatureMod/bin/Debug/net10.0/FullFeatureMod.dll --wait-for-debugger
```

如果使用 Rider、Visual Studio 或 VS Code：

- 启动程序选择 `ModAgent.DevHost` 或正式 App 的开发模式入口。
- 参数传入 `--dev --mod $(TargetPath) --wait-for-debugger`。
- 断点打在 `FullFeatureMod.cs` 中。
- 宿主加载 DLL 后，断点会在你的 Mod 代码里命中。

## 示例工具

### `sample_echo`

参数：

```json
{
  "text": "hello",
  "uppercase": true
}
```

返回：

```text
HELLO
```

### `sample_counter`

参数：

```json
{
  "step": 2
}
```

效果：

- 从宿主数据存储读取 `sample-counter`。
- 增加 `step`。
- 写回新值。
- 发布 `samples.full-feature.counter.changed` 事件。
- 返回当前计数器值。

## 示例模型

`DemoModelProvider` 暴露模型：

```text
samples.demo-model.echo
```

它会读取最后一条用户消息并返回：

```text
示例模型收到：用户输入
```

真实模型接入时，可以把这里替换为 HTTP API、本地模型、代理服务或企业内部模型网关。

## 示例 Hook

`DemoPipelineHook` 会：

- 在 `BeforeLoopAsync` 中向 `PipelineContext.Bag` 写入开始时间。
- 在 `AfterLoopAsync` 中记录总轮数。

Hook 适合做：

- 上下文注入。
- 审计记录。
- 成本统计。
- 策略拦截。
- 最终回复追加说明。

## 示例托管服务

`DemoHostedService` 会在宿主启动 Mod 后执行 `StartAsync`，并读取配置：

```text
samples.full-feature.intervalSeconds
```

真实场景可用于：

- 后台同步。
- 定时缓存刷新。
- 本地服务监听。
- 外部连接保活。

## 示例事件订阅

`DemoEventSubscriber` 订阅：

```text
samples.full-feature.counter.changed
```

真实场景可用于：

- Mod 间通信。
- UI 刷新通知。
- 审计事件处理。
- 后台任务触发。

## 注意事项

- 示例里使用 `Console.WriteLine` 只是为了说明事件处理，正式 Mod 推荐通过宿主日志服务记录。
- `CounterTool` 通过 `IToolExecutionContext.GetService<IModContext>()` 获取宿主上下文，真实宿主需要注入该服务。
- 示例不会访问闭源核心内部类型，所有能力都来自公开接口。
