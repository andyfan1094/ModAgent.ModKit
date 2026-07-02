# ModAgent ModKit：Mod 开发工具包

ModAgent ModKit 是给第三方开发者使用的开源 Mod 开发包。它只公开 Mod 开发所需的契约、SDK、测试工具和示例，不包含闭源宿主核心、桌面 UI、模型路由和商业逻辑。

## 项目定位

这是 ModAgent 的公开开发仓库，用来帮助第三方开发者在不接触闭源核心源码的情况下，编写、测试、调试和发布自己的 Mod。

## 包含内容

- `src/ModAgent.Abstractions`：Mod 与宿主之间的稳定接口契约。
- `src/ModAgent.Mod.Sdk`：Manifest、权限和打包校验辅助代码。
- `src/ModAgent.Mod.TestKit`：Mock Context、Fake DataStore、Fake EventBus，方便单元测试 Mod。
- `src/ModAgent.Mod.TestHost`：**本地 Mod 测试宿主**，加载 Mod DLL 并用 Mock Context 交互式测试，无需完整 ModAgent 宿主。
- `samples/HelloToolMod`：最小可运行工具 Mod 示例。
- `samples/FullFeatureMod`：覆盖工具、模型、Hook、托管服务、事件、配置页、定时任务、运行时和审计的全功能示例。
- `docs/`：开发、调试、发布和兼容性说明。

## 不包含内容

- `ModAgent.Core` 闭源实现。
- `ModAgent.Desktop` 桌面宿主源码。
- 模型路由、账单、权限执行、数据库和 UI 内部实现。
- 私有配置、密钥、用户数据和商业服务端代码。

## 快速开始

```bash
# 克隆仓库
git clone https://github.com/andyfan1094/ModAgent.ModKit.git
cd ModAgent.ModKit

# 构建
dotnet build

# 用 TestHost 测试示例 Mod
dotnet run --project src/ModAgent.Mod.TestHost -- samples/HelloToolMod/bin/Release/net10.0/HelloToolMod.dll --list

# 调用特定工具
dotnet run --project src/ModAgent.Mod.TestHost -- samples/HelloToolMod/bin/Release/net10.0/HelloToolMod.dll --tool=hello --args='{}'

# 交互模式
dotnet run --project src/ModAgent.Mod.TestHost -- samples/HelloToolMod/bin/Release/net10.0/HelloToolMod.dll
```

## TestHost 用法

TestHost 是一个轻量的本地 Mod 测试工具，开发者可以在不安装 ModAgent 完整宿主的情况下，加载和调试自己的 Mod。

```
用法: modagent-testhost <Mod.dll> [选项]

选项:
  --workdir=<路径>        工作目录（默认当前目录）
  --tool=<工具名>         指定要调用的工具
  --args=<JSON>           传给工具的 JSON 参数
  --list                  仅列出 Mod 信息
  --non-interactive       非交互模式
  --verbose               详细错误信息
```

交互模式下支持的命令：
- `list` — 列出所有工具
- `run <工具名> <JSON>` — 调用工具
- `quit` — 退出

## 本地调试 Mod

开发者在 IDE 中创建自己的 Mod 项目后，可以直接用 TestHost 测试：

```bash
# 1. 构建你的 Mod
dotnet build MyMod/MyMod.csproj

# 2. 用 TestHost 加载测试
dotnet run --project path/to/ModAgent.ModKit/src/ModAgent.Mod.TestHost -- MyMod/bin/Debug/net10.0/MyMod.dll
```

也可以在 IDE 中把闭源宿主配置为启动程序，并把当前 Mod 的 `$(TargetPath)` 传给 `--mod` 参数。
开源仓库的 GitHub Release 会提供 `ModAgent.DevHost-osx-arm64-<version>.zip`，里面包含闭源宿主二进制和官方 Mod 二进制，不包含核心源码。

## 文档入口

- `docs/mod-development-guide.md`：完整 Mod 开发、测试、调试和发布流程。
- `docs/full-feature-sample.md`：全功能示例 Mod 的能力说明和运行方式。
- `docs/closed-core-devhost-packaging.md`：核心闭源时如何打包 DevHost，避免泄露源码。
- `docs/devhost-runtime-package.md`：如何把闭源 DevHost 和官方 Mod 打成 Release 运行包。
- `docs/open-source-boundary.md`：开源边界和闭源核心保护策略。
- `docs/publishing.md`：公开仓库和 Release 发布建议。

## NuGet 发布

```bash
# 打包
dotnet pack src/ModAgent.Abstractions/ModAgent.Abstractions.csproj -c Release -o ./nupkgs
dotnet pack src/ModAgent.Mod.Sdk/ModAgent.Mod.Sdk.csproj -c Release -o ./nupkgs
dotnet pack src/ModAgent.Mod.TestKit/ModAgent.Mod.TestKit.csproj -c Release -o ./nupkgs

# 或用脚本一键发布
./scripts/publish-nuget.sh <NuGetApiKey>
```

## 开源边界

这个仓库的目标是让开发者能写、测、调试和发布 Mod，同时保护宿主核心实现。宿主只承诺 `ModAgent.Abstractions` 的语义化版本兼容性。

## 许可证

本仓库使用 MIT 许可证发布。许可证正文保留英文原文，以保证法律文本通用性。
