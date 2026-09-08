#!/usr/bin/env bash
set -euo pipefail

TASK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../lib/context.sh
source "$TASK_DIR/../../lib/context.sh"
# shellcheck source=../../lib/guards.sh
source "$TASK_DIR/../../lib/guards.sh"

require_command zip
require_command shasum
require_dir "$ADOMEJI_PACKAGE_STAGE"
assert_non_root_path "$ADOMEJI_PACKAGE_ZIP_PATH"

mkdir -p "$(dirname "$ADOMEJI_PACKAGE_ZIP_PATH")"
rm -f "$ADOMEJI_PACKAGE_ZIP_PATH"
(
  cd "$ADOMEJI_PACKAGE_ROOT"
  zip -qr "$ADOMEJI_PACKAGE_ZIP_PATH" Adomeji
)

package_bytes="$(wc -c < "$ADOMEJI_PACKAGE_ZIP_PATH" | tr -d ' ')"
package_sha256="$(shasum -a 256 "$ADOMEJI_PACKAGE_ZIP_PATH" | awk '{print $1}')"
cat > "$ADOMEJI_UPDATE_MANIFEST_PATH" <<EOF
{
  "schemaVersion": 1,
  "version": "$ADOMEJI_VERSION",
  "packageAsset": "Adomeji.zip",
  "packageBytes": $package_bytes,
  "packageSha256": "$package_sha256",
  "runtimePath": "Adomeji/Runtime/versions/$ADOMEJI_VERSION"
}
EOF

printf 'Created package: %s\n' "$ADOMEJI_PACKAGE_ZIP_PATH"
printf 'Created manifest: %s\n' "$ADOMEJI_UPDATE_MANIFEST_PATH"
