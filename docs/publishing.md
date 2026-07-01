# 发布到 GitHub

## 本地准备

```bash
dotnet build ModAgent.ModKit.slnx
git init
git add .
git commit -m "Initial open source ModKit"
```

## 创建远程仓库

如果使用 GitHub CLI：

```bash
gh repo create ModAgent.ModKit --public --source . --remote origin --push
```

如果要先保留私有：

```bash
gh repo create ModAgent.ModKit --private --source . --remote origin --push
```

## 发布 NuGet 包

```bash
dotnet pack src/ModAgent.Abstractions/ModAgent.Abstractions.csproj -c Release
dotnet pack src/ModAgent.Mod.Sdk/ModAgent.Mod.Sdk.csproj -c Release
dotnet pack src/ModAgent.Mod.TestKit/ModAgent.Mod.TestKit.csproj -c Release
```

发布包之前请确认版本号和 API 兼容性。
