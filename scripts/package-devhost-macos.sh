#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE_RUNTIME_DIR="${MODAGENT_RUNTIME_DIR:-$HOME/Library/Application Support/ModAgent}"
VERSION="${1:-dev}"
RID="${2:-osx-arm64}"
OUTPUT_DIR="$ROOT_DIR/artifacts/devhost-packages"
STAGE_PARENT="$OUTPUT_DIR/stage"
STAGE_DIR="$STAGE_PARENT/ModAgent.DevHost-$RID"
PACKAGE_NAME="ModAgent.DevHost-$RID-$VERSION.zip"
PACKAGE_PATH="$OUTPUT_DIR/$PACKAGE_NAME"

APP_SOURCE="$SOURCE_RUNTIME_DIR/ModAgent.app"
MODS_SOURCE="$SOURCE_RUNTIME_DIR/Mods"

if [[ ! -d "$APP_SOURCE" ]]; then
  echo "未找到 ModAgent.app：$APP_SOURCE" >&2
  exit 1
fi

if [[ ! -d "$MODS_SOURCE" ]]; then
  echo "未找到 Mods 目录：$MODS_SOURCE" >&2
  exit 1
fi

rm -rf "$STAGE_DIR" "$PACKAGE_PATH"
mkdir -p "$STAGE_DIR/Mods" "$STAGE_DIR/examples" "$OUTPUT_DIR"

RSYNC_EXCLUDES=(
  --exclude '*.pdb'
  --exclude '*.pdb.*'
  --exclude '*.mdb'
  --exclude '*.mdb.*'
  --exclude '*.cs'
  --exclude '*.cs.*'
  --exclude '*.csproj'
  --exclude '*.csproj.*'
  --exclude '*.sln'
  --exclude '*.sln.*'
  --exclude '*.slnx'
  --exclude '*.slnx.*'
  --exclude '*.nupkg'
  --exclude '*.nupkg.*'
  --exclude '*.snupkg'
  --exclude '*.snupkg.*'
  --exclude '*.bak'
  --exclude '*.bak.*'
  --exclude '*.*.bak'
  --exclude '*.*.bak.*'
  --exclude '.DS_Store'
)

rsync -a "${RSYNC_EXCLUDES[@]}" "$APP_SOURCE/" "$STAGE_DIR/ModAgent.app/"

for runtime_file in ModAgent.Core.dll ModAgent.Engine.dll ModAgent.Desktop.dll; do
  if [[ -f "$SOURCE_RUNTIME_DIR/$runtime_file" ]]; then
    cp -p "$SOURCE_RUNTIME_DIR/$runtime_file" "$STAGE_DIR/$runtime_file"
  fi
done

while IFS= read -r mod_file; do
  file_name="$(basename "$mod_file")"
  case "$file_name" in
    *.pdb|*.pdb.*|*.mdb|*.mdb.*|*.cs|*.cs.*|*.csproj|*.csproj.*|*.sln|*.sln.*|*.slnx|*.slnx.*|*.nupkg|*.nupkg.*|*.snupkg|*.snupkg.*|*.bak|*.bak.*|*.*.bak|*.*.bak.*|.DS_Store)
      continue
      ;;
  esac
  cp -p "$mod_file" "$STAGE_DIR/Mods/$file_name"
done < <(find "$MODS_SOURCE" -maxdepth 1 -type f \( -name '*.dll' -o -name '*.deps.json' -o -name '*.runtimeconfig.json' -o -name '*.dylib' -o -name '*.so' \) | sort)

APP_MODS_DIR="$STAGE_DIR/ModAgent.app/Contents/MacOS/Mods"
rm -rf "$APP_MODS_DIR"
mkdir -p "$APP_MODS_DIR"
rsync -a "$STAGE_DIR/Mods/" "$APP_MODS_DIR/"

cat > "$STAGE_DIR/install-official-mods.command" <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail
PACKAGE_DIR="$(cd "$(dirname "$0")" && pwd)"
TARGET_DIR="$HOME/Library/Application Support/ModAgent/Mods"
mkdir -p "$TARGET_DIR"
rsync -a --delete "$PACKAGE_DIR/Mods/" "$TARGET_DIR/"
echo "官方 Mod 已安装到：$TARGET_DIR"
SCRIPT
chmod +x "$STAGE_DIR/install-official-mods.command"

cat > "$STAGE_DIR/run-devhost.command" <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail
PACKAGE_DIR="$(cd "$(dirname "$0")" && pwd)"
"$PACKAGE_DIR/install-official-mods.command"
exec "$PACKAGE_DIR/ModAgent.app/Contents/MacOS/ModAgent" "$@"
SCRIPT
chmod +x "$STAGE_DIR/run-devhost.command"

cat > "$STAGE_DIR/README-中文.txt" <<README
ModAgent DevHost 运行包

用途：
- 给 Mod 开发者运行闭源宿主。
- 包含官方 Mod 二进制和运行依赖。
- 不包含核心源码、调试符号、私有配置、用户数据或密钥。

快速运行：
1. 解压本 zip。
2. 执行 ./install-official-mods.command 安装官方 Mod。
3. 执行 ./run-devhost.command 启动宿主。
4. 将自己编译好的 Mod DLL 放入：
   \$HOME/Library/Application Support/ModAgent/Mods
5. 在 IDE 中调试自己的 Mod 项目，断点打在自己的 Mod 源码里。

如宿主支持开发参数，也可以把参数传给运行脚本，例如：
  ./run-devhost.command --dev --mod /path/to/MyMod.dll --wait-for-debugger

注意：
- 本包只授权用于 Mod 开发、测试和调试。
- 请不要反编译、破解或重新分发闭源核心二进制。
- 如 macOS 提示来自未知开发者，可在系统设置中允许运行，或对解压目录执行：
  xattr -dr com.apple.quarantine /path/to/ModAgent.DevHost-$RID
README

cat > "$STAGE_DIR/manifest.json" <<EOF
{
  "name": "ModAgent DevHost",
  "version": "$VERSION",
  "runtimeIdentifier": "$RID",
  "containsCoreSource": false,
  "containsDebugSymbols": false,
  "containsOfficialMods": true,
  "entrypoint": "run-devhost.command",
  "appPath": "ModAgent.app/Contents/MacOS/ModAgent",
  "officialModsPath": "Mods",
  "licenseNote": "仅用于 Mod 开发、测试和调试；不包含核心源码。"
}
EOF

if find "$STAGE_DIR" \( \
  -name '*.pdb' -o -name '*.pdb.*' -o \
  -name '*.mdb' -o -name '*.mdb.*' -o \
  -name '*.cs' -o -name '*.cs.*' -o \
  -name '*.csproj' -o -name '*.csproj.*' -o \
  -name '*.sln' -o -name '*.sln.*' -o \
  -name '*.slnx' -o -name '*.slnx.*' -o \
  -name '*.nupkg' -o -name '*.nupkg.*' -o \
  -name '*.snupkg' -o -name '*.snupkg.*' -o \
  -name '*.bak' -o -name '*.bak.*' -o -name '*.*.bak' -o -name '*.*.bak.*' \
\) -print -quit | grep -q .; then
  echo "打包结果包含不应发布的源码、符号、包文件或备份文件" >&2
  exit 1
fi

(
  cd "$STAGE_PARENT"
  zip -qry "../$PACKAGE_NAME" "ModAgent.DevHost-$RID"
)

rm -rf "$STAGE_PARENT"

echo "$PACKAGE_PATH"
