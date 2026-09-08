#!/usr/bin/env bash
set -euo pipefail

TASK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../lib/context.sh
source "$TASK_DIR/../../lib/context.sh"
# shellcheck source=../../lib/guards.sh
source "$TASK_DIR/../../lib/guards.sh"

require_file "$ADOMEJI_PACKAGE_ZIP_PATH"
require_command unzip
require_command shasum

entries="$(unzip -Z1 "$ADOMEJI_PACKAGE_ZIP_PATH")"
for required in \
  "Adomeji/Adomeji.Bootstrap.dll" \
  "Adomeji/Info.json" \
  "Adomeji/UpdateSettings.json" \
  "Adomeji/Runtime/state.json" \
  "Adomeji/Runtime/versions/$ADOMEJI_VERSION/Adomeji.dll" \
  "Adomeji/Runtime/versions/$ADOMEJI_VERSION/Adomeji.UpdateEngine.dll" \
  "Adomeji/LICENSE.upstream-adomeji" \
  "Adomeji/THIRD_PARTY_NOTICES.md" \
  "Adomeji/Runtime/versions/$ADOMEJI_VERSION/Sprites/ELLIE/walk0.png"; do
  if ! printf '%s\n' "$entries" | grep -Fxq "$required"; then
    printf 'Package is missing required entry: %s\n' "$required" >&2
    exit 1
  fi
done

dll_count="$(printf '%s\n' "$entries" | grep -Ec '\.dll$')"
if [ "$dll_count" -ne 3 ]; then
  printf 'Package must contain bootstrap, update engine, and runtime DLLs; found %s.\n' "$dll_count" >&2
  exit 1
fi

ellie_count="$(printf '%s\n' "$entries" | grep -Ec "^Adomeji/Runtime/versions/$ADOMEJI_VERSION/Sprites/ELLIE/[^/]+\\.png$")"
if [ "$ellie_count" -ne 36 ]; then
  printf 'Package must contain 36 ELLIE PNG files, found %s.\n' "$ellie_count" >&2
  exit 1
fi

require_file "$ADOMEJI_UPDATE_MANIFEST_PATH"
manifest_bytes="$(sed -n 's/.*"packageBytes": \([0-9][0-9]*\).*/\1/p' "$ADOMEJI_UPDATE_MANIFEST_PATH")"
manifest_sha="$(sed -n 's/.*"packageSha256": "\([0-9a-fA-F][0-9a-fA-F]*\)".*/\1/p' "$ADOMEJI_UPDATE_MANIFEST_PATH")"
actual_bytes="$(wc -c < "$ADOMEJI_PACKAGE_ZIP_PATH" | tr -d ' ')"
actual_sha="$(shasum -a 256 "$ADOMEJI_PACKAGE_ZIP_PATH" | awk '{print $1}')"
if [ "$manifest_bytes" != "$actual_bytes" ] || [ "$manifest_sha" != "$actual_sha" ]; then
  printf 'Update manifest size or checksum does not match Adomeji.zip.\n' >&2
  exit 1
fi
printf 'Package contents verified: versioned runtime and %s ELLIE sprites.\n' "$ellie_count"
