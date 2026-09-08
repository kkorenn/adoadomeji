#!/usr/bin/env bash
set -euo pipefail

TASK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../lib/context.sh
source "$TASK_DIR/../../lib/context.sh"

DOTNET_ROOT="$DOTNET_ROOT" \
  "$DOTNET_EXE" run \
    --project "$ADOMEJI_PROJECT_ROOT/Adomeji.Tests/Adomeji.Tests.csproj" \
    --configuration Release

DOTNET_ROOT="$DOTNET_ROOT" \
  "$DOTNET_EXE" run \
    --project "$ADOMEJI_PROJECT_ROOT/Adomeji.Updater.Tests/Adomeji.Updater.Tests.csproj" \
    --configuration Release \
    -p:AdofaiManaged="$ADOFAI_MANAGED" \
    -p:UnityModManagerDll="$UNITY_MOD_MANAGER_DLL"
