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
assert_child_path "$ADOMEJI_PACKAGE_STAGE" "$ADOMEJI_PACKAGE_ROOT"

if [ -e "$ADOMEJI_PACKAGE_STAGE" ]; then
  safe_remove_tree "$ADOMEJI_PACKAGE_STAGE" "$ADOMEJI_PACKAGE_ROOT"
fi
mkdir -p "$ADOMEJI_PACKAGE_STAGE"

cp "$ADOMEJI_BOOTSTRAP_BUILD_OUTPUT/Adomeji.Bootstrap.dll" "$ADOMEJI_PACKAGE_STAGE/"
cp "$ADOMEJI_PROJECT_ROOT/Adomeji/Info.json" "$ADOMEJI_PACKAGE_STAGE/"
cp "$ADOMEJI_PROJECT_ROOT/Adomeji/UpdateSettings.json" "$ADOMEJI_PACKAGE_STAGE/"
cp "$ADOMEJI_PROJECT_ROOT/LICENSE.upstream-adomeji" "$ADOMEJI_PACKAGE_STAGE/"
cp "$ADOMEJI_PROJECT_ROOT/THIRD_PARTY_NOTICES.md" "$ADOMEJI_PACKAGE_STAGE/"

runtime="$ADOMEJI_PACKAGE_STAGE/Runtime/versions/$ADOMEJI_VERSION"
mkdir -p "$runtime"
cp "$ADOMEJI_BUILD_OUTPUT/Adomeji.dll" "$runtime/"
cp "$ADOMEJI_UPDATE_ENGINE_BUILD_OUTPUT/Adomeji.UpdateEngine.dll" "$runtime/"
cp "$ADOMEJI_PROJECT_ROOT/Adomeji/Info.json" "$runtime/"
if [ -d "$ADOMEJI_PROJECT_ROOT/Adomeji/Content/Sprites" ]; then
  cp -R "$ADOMEJI_PROJECT_ROOT/Adomeji/Content/Sprites" "$runtime/Sprites"
fi
cat > "$ADOMEJI_PACKAGE_STAGE/Runtime/state.json" <<EOF
{
  "SchemaVersion": 1,
  "Current": "$ADOMEJI_VERSION",
  "Previous": null,
  "Trial": null
}
EOF
