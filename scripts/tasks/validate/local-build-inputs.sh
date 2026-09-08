#!/usr/bin/env bash
set -euo pipefail

TASK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../lib/context.sh
source "$TASK_DIR/../../lib/context.sh"
# shellcheck source=../../lib/guards.sh
source "$TASK_DIR/../../lib/guards.sh"

require_executable "$DOTNET_EXE"
require_dir "$ADOFAI_MANAGED"
require_file "$UNITY_MOD_MANAGER_DLL"
require_file "$ADOFAI_MANAGED/Assembly-CSharp.dll"
require_file "$ADOFAI_MANAGED/Assembly-CSharp-firstpass.dll"
require_file "$ADOMEJI_PROJECT_ROOT/Adomeji/Adomeji.csproj"
require_file "$ADOMEJI_PROJECT_ROOT/Adomeji/Info.json"
require_dir "$ADOMEJI_PROJECT_ROOT/Adomeji/Content/Sprites/ELLIE"
require_file "$ADOMEJI_PROJECT_ROOT/LICENSE.upstream-adomeji"
require_file "$ADOMEJI_PROJECT_ROOT/THIRD_PARTY_NOTICES.md"
