#!/usr/bin/env bash
set -euo pipefail

TASK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../lib/context.sh
source "$TASK_DIR/../../lib/context.sh"

ui_root="$ADOMEJI_PROJECT_ROOT/Adomeji/UI"

if [ -e "$ui_root/SettingsWindow.cs" ]; then
  printf 'Legacy monolithic SettingsWindow.cs must not exist.\n' >&2
  exit 1
fi

if grep -R -n --include='*.cs' 'void Update(' "$ui_root"; then
  printf 'UI components must be driven by ISettingsWindow.Tick, not MonoBehaviour.Update.\n' >&2
  exit 1
fi

if grep -R -n --include='*.cs' 'partial class' "$ui_root"; then
  printf 'UI responsibilities must not be split with partial classes.\n' >&2
  exit 1
fi

while IFS= read -r file; do
  lines="$(wc -l < "$file")"
  if [ "$lines" -gt 800 ]; then
    printf 'UI component exceeds 800 lines (%s): %s\n' "$lines" "$file" >&2
    exit 1
  fi
done < <(find "$ui_root" -type f -name '*.cs' | sort)

printf 'UI architecture verified: instance components, no Update loop, no partial monolith.\n'
