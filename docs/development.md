# 开发指南

## 设计原则

ModAgent 的公开开发面只依赖 `ModAgent.Abstractions`。第三方 Mod 不应该引用宿主核心、桌面 UI、数据库实现或内部服务类型。

## Mod 生命周期

1. 宿主扫描 DLL。
2. 查找带 `[ModEntry]` 且实现 `IMod` 的入口类型。
3. 读取 `IMod.Manifest` 并检查版本、依赖和权限。
4. 调用 `InitializeAsync(IModContext)`。
5. 注册工具、模型、管道钩子、服务、配置页和定时任务。
6. 宿主退出或禁用 Mod 时释放对应服务。

## 能力声明

Manifest 中需要声明能力和权限：

- `ModCapability.Tools`：提供工具。
- `ModCapability.ModelProviders`：提供模型后端。
- `ModCapability.PipelineHooks`：插入对话流程。
- `ModCapability.Services`：向其他 Mod 暴露服务。
- `ModCapability.ConfigPage`：提供配置页面。

权限建议使用短字符串，例如：

- `network.http`
- `filesystem.read`
- `filesystem.write`
- `browser.control`
- `models.invoke`
- `tools.execute`

## 调试

开发者可以用闭源 DevHost 二进制加载当前 Mod：

```bash
modagent dev --mod ./bin/Debug/net10.0/MyPlugin.dll
```

IDE 调试时，把宿主二进制设为启动程序，把 `--dev --mod $(TargetPath)` 作为参数。断点只需要打在自己的 Mod 项目里。
