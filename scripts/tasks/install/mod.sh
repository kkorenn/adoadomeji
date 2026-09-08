#!/usr/bin/env bash
set -euo pipefail

TASK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../lib/context.sh
source "$TASK_DIR/../../lib/context.sh"
# shellcheck source=../../lib/guards.sh
source "$TASK_DIR/../../lib/guards.sh"

require_file "$ADOMEJI_BUILD_OUTPUT/Adomeji.dll"
require_file "$ADOMEJI_BOOTSTRAP_BUILD_OUTPUT/Adomeji.Bootstrap.dll"
require_file "$ADOMEJI_UPDATE_ENGINE_BUILD_OUTPUT/Adomeji.UpdateEngine.dll"
require_file "$ADOMEJI_PROJECT_ROOT/Adomeji/Info.json"
require_file "$ADOMEJI_PROJECT_ROOT/LICENSE.upstream-adomeji"
require_file "$ADOMEJI_PROJECT_ROOT/THIRD_PARTY_NOTICES.md"
assert_non_root_path "$ADOMEJI_INSTALL_PATH"

mkdir -p "$ADOMEJI_INSTALL_PATH"
cp "$ADOMEJI_BOOTSTRAP_BUILD_OUTPUT/Adomeji.Bootstrap.dll" "$ADOMEJI_INSTALL_PATH/"
cp "$ADOMEJI_PROJECT_ROOT/Adomeji/Info.json" "$ADOMEJI_INSTALL_PATH/"
cp "$ADOMEJI_PROJECT_ROOT/LICENSE.upstream-adomeji" "$ADOMEJI_INSTALL_PATH/"
cp "$ADOMEJI_PROJECT_ROOT/THIRD_PARTY_NOTICES.md" "$ADOMEJI_INSTALL_PATH/"
rm -f "$ADOMEJI_INSTALL_PATH/Adomeji.dll" "$ADOMEJI_INSTALL_PATH/Adomeji.pdb"

runtime_root="$ADOMEJI_INSTALL_PATH/Runtime"
runtime_target="$runtime_root/versions/$ADOMEJI_VERSION"
mkdir -p "$runtime_root/versions"
if [ -e "$runtime_target" ]; then
  safe_remove_tree "$runtime_target" "$runtime_root/versions"
fi
mkdir -p "$runtime_target"
cp "$ADOMEJI_BUILD_OUTPUT/Adomeji.dll" "$runtime_target/"
cp "$ADOMEJI_UPDATE_ENGINE_BUILD_OUTPUT/Adomeji.UpdateEngine.dll" "$runtime_target/"
cp "$ADOMEJI_PROJECT_ROOT/Adomeji/Info.json" "$runtime_target/"

if [ ! -f "$ADOMEJI_INSTALL_PATH/UpdateSettings.json" ]; then
  cp "$ADOMEJI_PROJECT_ROOT/Adomeji/UpdateSettings.json" "$ADOMEJI_INSTALL_PATH/"
fi

if [ ! -f "$runtime_root/state.json" ]; then
cat > "$runtime_root/state.json" <<EOF
{
  "SchemaVersion": 1,
  "Current": "$ADOMEJI_VERSION",
  "Previous": null,
  "Trial": null
}
EOF
fi

content_sprites="$ADOMEJI_PROJECT_ROOT/Adomeji/Content/Sprites"
if [ -d "$content_sprites" ]; then
  mkdir -p "$runtime_target/Sprites"
  for pack in "$content_sprites"/*; do
    [ -d "$pack" ] || continue
    pack_name="$(basename "$pack")"
    target="$runtime_target/Sprites/$pack_name"
    cp -R "$pack" "$target"
  done
fi

printf 'Installed to %s\n' "$ADOMEJI_INSTALL_PATH"
