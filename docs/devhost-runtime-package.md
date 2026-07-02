# 运行包与官方 Mod 调试指南

本文说明如何在开源仓库中提供闭源运行包，让 Mod 开发者可以直接运行宿主、加载官方 Mod，并调试自己的 Mod，同时不泄露核心源码。

## 发布策略

- Git 仓库只提交源码开放部分：`Abstractions`、`Mod.Sdk`、`TestKit`、示例和文档。
- GitHub Release 上传闭源运行包：`ModAgent.DevHost-osx-arm64-<version>.zip`。
- 运行包包含 `ModAgent.app`、`ModAgent.Core.dll` 和 `Mods/` 官方 Mod 二进制。
- 运行包不包含核心源码、`.pdb`、`.mdb`、SourceLink、私有配置、密钥和用户数据。

## 运行包目录

```text
ModAgent.DevHost-osx-arm64-<version>.zip
  ModAgent.DevHost-osx-arm64/
    ModAgent.app/
    ModAgent.Core.dll
    Mods/
      ModAgent.Mods.Browser.dll
      ModAgent.Mods.BuiltIn.dll
      ModAgent.Mods.Git.dll
      ModAgent.Mods.Memory.dll
      ModAgent.Mods.ModelRouter.dll
      ModAgent.Mods.ProjectPlanner.dll
      ModAgent.Mods.SelfUpgrade.dll
      ...
    examples/
    install-official-mods.command
    run-devhost.command
    manifest.json
    README-中文.txt
```

## 开发者使用方式

开发者克隆开源仓库：

```bash
git clone https://github.com/andyfan1094/ModAgent.ModKit.git
cd ModAgent.ModKit
```

构建示例 Mod：

```bash
dotnet build samples/HelloToolMod/HelloToolMod.csproj -c Debug
dotnet build samples/FullFeatureMod/FullFeatureMod.csproj -c Debug
```

从 GitHub Release 下载并解压 `ModAgent.DevHost-osx-arm64-<version>.zip`，然后用运行脚本启动宿主：

```bash
./ModAgent.DevHost-osx-arm64/run-devhost.command
```

运行脚本会先把包内官方 Mod 安装到当前用户的 ModAgent `Mods` 目录，再启动闭源宿主。也可以直接带上自己的 Mod 调试参数：

```bash
./ModAgent.DevHost-osx-arm64/run-devhost.command --dev --mod "$PWD/samples/HelloToolMod/bin/Debug/net10.0/HelloToolMod.dll" --wait-for-debugger
```

如果 macOS 提示未知开发者或隔离属性，可以对本地解压目录执行：

```bash
xattr -dr com.apple.quarantine ./ModAgent.DevHost-osx-arm64
```

## 调试自己的 Mod

推荐流程：

1. 用 Debug 配置编译自己的 Mod。
2. 将 Mod DLL 复制到宿主可加载的 Mods 目录，或使用宿主开发模式加载 DLL。
3. IDE 启动程序指向运行包里的 `ModAgent.app/Contents/MacOS/ModAgent`。
4. 断点打在自己的 Mod 源码里。
5. 宿主加载 Mod 后，断点会命中开发者自己的代码。

IDE 启动配置示例：

```json
{
  "executablePath": "./ModAgent.DevHost-osx-arm64/ModAgent.app/Contents/MacOS/ModAgent",
  "commandLineArgs": "--dev --mod $(TargetPath) --wait-for-debugger"
}
```

如果当前宿主版本尚未实现 `--dev --mod` 参数，也可以先把 DLL 放入宿主默认 Mods 目录进行加载测试。

## 维护者打包方式

在拥有闭源宿主产物的机器上运行：

```bash
bash scripts/package-devhost-macos.sh v1.0.0 osx-arm64
```

脚本默认从当前用户的 ModAgent 运行目录读取产物：

```text
~/Library/Application Support/ModAgent/ModAgent.app
~/Library/Application Support/ModAgent/ModAgent.Core.dll
~/Library/Application Support/ModAgent/Mods/
```

也可以指定运行目录：

```bash
MODAGENT_RUNTIME_DIR="/path/to/ModAgentRuntime" bash scripts/package-devhost-macos.sh v1.0.0 osx-arm64
```

生成结果位于：

```text
artifacts/devhost-packages/ModAgent.DevHost-osx-arm64-v1.0.0.zip
```

## 发布到 GitHub Release

```bash
gh release create v1.0.0 \
  artifacts/devhost-packages/ModAgent.DevHost-osx-arm64-v1.0.0.zip \
  --title "ModAgent ModKit v1.0.0" \
  --notes "提供 Mod 开发 SDK、中文文档、示例 Mod，以及闭源 DevHost 运行包。运行包包含官方 Mod 二进制，不包含核心源码和调试符号。"
```

如果 Release 已存在，可以追加上传：

```bash
gh release upload v1.0.0 \
  artifacts/devhost-packages/ModAgent.DevHost-osx-arm64-v1.0.0.zip \
  --clobber
```

## 安全检查

打包脚本会自动排除并检查以下文件：

- `*.pdb`
- `*.mdb`
- `*.cs`
- `*.csproj`
- `*.sln`
- `*.slnx`
- `*.nupkg`
- `*.snupkg`
- `*.bak`
- `*.pdb.*`
- `*.cs.*`

发布前还应确认：

- 运行包里没有用户数据目录。
- 运行包里没有 token、密钥、数据库和私有配置。
- 日志不会输出核心源码路径或敏感参数。
- 官方 Mod 只包含可分发的 DLL 和运行依赖。

## 为什么不直接提交二进制到 Git

`ModAgent.app` 和官方 Mod 体积较大，直接提交到 Git 会造成仓库膨胀、克隆变慢，也不利于版本管理。推荐把大二进制放在 GitHub Release，仓库只保留脚本、文档和开源 SDK。这样开发者仍然可以从同一个开源项目入口下载运行包并调试 Mod。
