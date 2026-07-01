# 核心闭源 DevHost 打包方案

本文说明在不公开核心源码的前提下，如何给 Mod 开发者提供可测试、可调试的 DevHost 二进制。

## 目标

- 开发者可以编写、测试、调试自己的 Mod。
- 开发者不能看到 `ModAgent.Core`、桌面 UI、模型路由、权限执行、账单和数据层源码。
- DevHost 的行为尽量接近真实 App，减少“测试通过、上线失败”。
- Release 包不包含源码、SourceLink、调试符号和敏感配置。

## 推荐发布结构

```text
ModAgent.DevHost-osx-arm64.zip
  ModAgent.DevHost
  ModAgent.Abstractions.dll
  ModAgent.Core.dll
  ModAgent.AppHost.dll
  Mods/
  examples/
  logs/
  README.txt
```

其中：

- `ModAgent.DevHost`：闭源可执行文件。
- `ModAgent.Core.dll`：闭源核心二进制。
- `ModAgent.AppHost.dll`：闭源宿主适配层。
- `ModAgent.Abstractions.dll`：公开接口 DLL，可与 NuGet 包一致。
- `Mods/`：开发者放置或临时加载 Mod 的目录。
- `examples/`：可选，只放公开示例，不放核心源码。

## 发布命令示例

macOS Apple Silicon：

```bash
dotnet publish src/ModAgent.DevHost/ModAgent.DevHost.csproj \
  -c Release \
  -r osx-arm64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:DebugType=none \
  -p:DebugSymbols=false \
  -p:PublishRepositoryUrl=false \
  -p:EmbedUntrackedSources=false \
  -p:ContinuousIntegrationBuild=false
```

Windows x64：

```bash
dotnet publish src/ModAgent.DevHost/ModAgent.DevHost.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:DebugType=none \
  -p:DebugSymbols=false
```

Linux x64：

```bash
dotnet publish src/ModAgent.DevHost/ModAgent.DevHost.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:DebugType=none \
  -p:DebugSymbols=false
```

## 防泄露清单

发布前检查 Release 目录，确保没有：

- `.cs` 源码文件。
- `.pdb`、`.mdb` 调试符号。
- `.sln`、`.csproj`、`.props`、`.targets` 等核心工程文件。
- SourceLink 信息。
- 私钥、Token、Cookie、连接串。
- 用户数据、缓存、日志历史。
- 内部测试配置和商业服务地址。

可以执行：

```bash
find publish -name "*.cs" -o -name "*.pdb" -o -name "*.csproj" -o -name "*.sln"
```

结果应该为空，或只包含明确允许公开的文件。

## 调试体验

开发者调试自己的 Mod：

```bash
modagent dev --mod ./bin/Debug/net10.0/MyMod.dll --wait-for-debugger
```

IDE 配置：

```json
{
  "executablePath": "/Applications/ModAgent.DevHost/ModAgent.DevHost",
  "commandLineArgs": "--mod $(TargetPath) --wait-for-debugger"
}
```

断点只会命中开发者自己的 Mod 源码。闭源核心没有符号文件，因此不会暴露核心源码路径和内部实现。

## 混淆建议

可以对闭源核心 DLL 做混淆或裁剪，但要注意：

- 不要混淆 `ModAgent.Abstractions` 的公开类型名和成员名。
- 不要破坏反射发现 `[ModEntry]` 的逻辑。
- 不要裁剪宿主通过反射加载 Mod 所需的代码。
- 混淆前后都要运行官方示例 Mod 的加载测试。

## 日志建议

DevHost 日志要对开发者友好，但不能泄露内部实现：

应该输出：

- Mod DLL 路径。
- Manifest 校验结果。
- 权限申请结果。
- 依赖缺失信息。
- 公开接口层异常。

避免输出：

- 核心内部类完整调用链。
- 源码绝对路径。
- 私有配置。
- 用户密钥和令牌。
- 商业服务内部接口。

## GitHub Release 发布建议

开源仓库只放 ModKit 源码。Release 可以附加闭源 DevHost 二进制：

```bash
gh release create v1.0.0 \
  ModAgent.DevHost-osx-arm64.zip \
  ModAgent.DevHost-win-x64.zip \
  ModAgent.DevHost-linux-x64.zip \
  --title "ModAgent ModKit v1.0.0" \
  --notes "提供 Mod 开发接口、示例和闭源 DevHost 二进制。"
```

如果不希望 DevHost 放在开源仓库 Release，也可以放到官网或私有下载渠道，文档中只写下载地址。
