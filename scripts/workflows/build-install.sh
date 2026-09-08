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
run_task "Build mod (Debug)" "$TASKS_DIR/build/mod.sh" Debug
run_task "Run unit tests" "$TASKS_DIR/test/unit.sh"
run_task "Validate Unity/Mono compatibility" \
  "$TASKS_DIR/validate/unity-mono-compatibility.sh" \
  "$ADOMEJI_BUILD_OUTPUT/Adomeji.dll" \
  "$ADOMEJI_BOOTSTRAP_BUILD_OUTPUT/Adomeji.Bootstrap.dll" \
  "$ADOMEJI_UPDATE_ENGINE_BUILD_OUTPUT/Adomeji.UpdateEngine.dll"
run_task "Install mod" "$TASKS_DIR/install/mod.sh"
