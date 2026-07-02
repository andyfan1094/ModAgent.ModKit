# ModAgent ModKit：Mod 开发工具包

ModAgent ModKit 是给第三方开发者使用的开源 Mod 开发包。它只公开 Mod 开发所需的契约、SDK、测试工具和示例，不包含闭源宿主核心、桌面 UI、模型路由和商业逻辑。

## 项目定位

这是 ModAgent 的公开开发仓库，用来帮助第三方开发者在不接触闭源核心源码的情况下，编写、测试、调试和发布自己的 Mod。

## 包含内容

- `src/ModAgent.Abstractions`：Mod 与宿主之间的稳定接口契约。
- `src/ModAgent.Mod.Sdk`：Manifest、权限和打包校验辅助代码。
- `src/ModAgent.Mod.TestKit`：Mock Context、Fake DataStore、Fake EventBus，方便单元测试 Mod。
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
git clone https://github.com/andyfan1094/ModAgent.ModKit.git
cd ModAgent.ModKit
dotnet build src/ModAgent.Abstractions/ModAgent.Abstractions.csproj
dotnet build samples/HelloToolMod/HelloToolMod.csproj
dotnet build samples/FullFeatureMod/FullFeatureMod.csproj
```

开发者可以在自己的 Mod 项目里引用公开包：

```xml
<PackageReference Include="ModAgent.Abstractions" Version="1.0.0" />
<PackageReference Include="ModAgent.Mod.Sdk" Version="1.0.0" PrivateAssets="all" />
<PackageReference Include="ModAgent.Mod.TestKit" Version="1.0.0" PrivateAssets="all" />
```

## 调试方式

正式宿主闭源发布为二进制，开发者通过 DevHost 或正式 App 的开发模式加载自己的 DLL：

```bash
./run-devhost.command --dev --mod ./bin/Debug/net10.0/MyPlugin.dll --wait-for-debugger
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

## 开源边界

这个仓库的目标是让开发者能写、测、调试和发布 Mod，同时保护宿主核心实现。宿主只承诺 `ModAgent.Abstractions` 的语义化版本兼容性。

## 许可证

本仓库使用 MIT 许可证发布。许可证正文保留英文原文，以保证法律文本通用性。
