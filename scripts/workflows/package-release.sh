#!/usr/bin/env bash
set -euo pipefail

WORKFLOW_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPTS_DIR="$(cd "$WORKFLOW_DIR/.." && pwd)"
TASKS_DIR="$SCRIPTS_DIR/tasks"
# shellcheck source=../lib/context.sh
source "$SCRIPTS_DIR/lib/context.sh"
# shellcheck source=../lib/logging.sh
source "$SCRIPTS_DIR/lib/logging.sh"

run_task "Validate local build inputs" "$TASKS_DIR/validate/local-build-inputs.sh"
run_task "Validate UI architecture" "$TASKS_DIR/validate/ui-architecture.sh"
run_task "Build mod (Release)" "$TASKS_DIR/build/mod.sh" Release
run_task "Run unit tests" "$TASKS_DIR/test/unit.sh"
run_task "Validate Unity/Mono compatibility" \
  "$TASKS_DIR/validate/unity-mono-compatibility.sh" \
  "$ADOMEJI_BUILD_OUTPUT/Adomeji.dll" \
  "$ADOMEJI_BOOTSTRAP_BUILD_OUTPUT/Adomeji.Bootstrap.dll" \
  "$ADOMEJI_UPDATE_ENGINE_BUILD_OUTPUT/Adomeji.UpdateEngine.dll"
run_task "Stage release package" "$TASKS_DIR/package/stage.sh"
run_task "Archive release package" "$TASKS_DIR/package/archive.sh"
run_task "Validate release contents" "$TASKS_DIR/validate/package-contents.sh"
run_task "Validate packaged Unity/Mono compatibility" \
  "$TASKS_DIR/validate/unity-mono-compatibility.sh" \
  "$ADOMEJI_PACKAGE_ZIP_PATH"
