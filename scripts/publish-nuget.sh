#!/bin/bash
# ============================================================
# ModAgent.ModKit NuGet 发布脚本
# 用法: ./scripts/publish-nuget.sh <NuGetApiKey> [--dry-run]
# ============================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"
NUPKG_DIR="$REPO_DIR/nupkgs"
DRY_RUN=false

if [[ "${1:-}" == "--dry-run" ]]; then
    DRY_RUN=true
    API_KEY="dry-run"
else
    API_KEY="${1:-}"
    if [[ "${2:-}" == "--dry-run" ]]; then
        DRY_RUN=true
    fi
fi

if [[ -z "$API_KEY" ]]; then
    echo "用法: $0 <NuGetApiKey> [--dry-run]"
    echo "  NuGetApiKey   nuget.org 的 API Key"
    echo "  --dry-run     只打包不推送"
    exit 1
fi

echo "📦 1/4 清理旧包..."
rm -rf "$NUPKG_DIR"
mkdir -p "$NUPKG_DIR"

echo "🔨 2/4 构建并打包..."
cd "$REPO_DIR"
dotnet pack src/ModAgent.Abstractions/ModAgent.Abstractions.csproj -c Release -o "$NUPKG_DIR" /p:ContinuousIntegrationBuild=true
dotnet pack src/ModAgent.Mod.Sdk/ModAgent.Mod.Sdk.csproj           -c Release -o "$NUPKG_DIR" /p:ContinuousIntegrationBuild=true
dotnet pack src/ModAgent.Mod.TestKit/ModAgent.Mod.TestKit.csproj   -c Release -o "$NUPKG_DIR" /p:ContinuousIntegrationBuild=true

echo ""
echo "📋 3/4 已生成的包:"
ls -lh "$NUPKG_DIR"/*.nupkg

if $DRY_RUN; then
    echo ""
    echo "🏁 --dry-run 模式，跳过推送。包在: $NUPKG_DIR"
    exit 0
fi

echo ""
echo "🚀 4/4 推送到 nuget.org..."
for pkg in "$NUPKG_DIR"/*.nupkg; do
    echo "   推送: $(basename "$pkg")"
    dotnet nuget push "$pkg" --api-key "$API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate
done

echo ""
echo "✅ 发布完成！"
echo "   验证: https://www.nuget.org/packages/ModAgent.Abstractions"
echo "   验证: https://www.nuget.org/packages/ModAgent.Mod.Sdk"
echo "   验证: https://www.nuget.org/packages/ModAgent.Mod.TestKit"
