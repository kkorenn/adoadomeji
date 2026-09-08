#!/usr/bin/env bash
set -euo pipefail

WORKFLOW_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPTS_DIR="$(cd "$WORKFLOW_DIR/.." && pwd)"
TASKS_DIR="$SCRIPTS_DIR/tasks"
# shellcheck source=../lib/logging.sh
source "$SCRIPTS_DIR/lib/logging.sh"

run_task "Validate shell scripts" "$SCRIPTS_DIR/tasks/validate/shell-scripts.sh"
run_task "Validate UI architecture" "$TASKS_DIR/validate/ui-architecture.sh"
run_task "Run unit tests" "$TASKS_DIR/test/unit.sh"
