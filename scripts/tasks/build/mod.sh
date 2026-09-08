#!/usr/bin/env bash
set -euo pipefail

TASK_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
# shellcheck source=../../lib/context.sh
source "$TASK_DIR/../../lib/context.sh"

configuration="${1:-Debug}"

DOTNET_ROOT="$DOTNET_ROOT" \
  "$DOTNET_EXE" build "$ADOMEJI_PROJECT_ROOT/Adomeji/Adomeji.csproj" \
    --configuration "$configuration" \
    -p:OutputPath="$ADOMEJI_BUILD_OUTPUT/" \
    -p:AdofaiManaged="$ADOFAI_MANAGED" \
    -p:AdofaiMods="$ADOFAI_MODS_DIR" \
    -p:UnityModManagerDll="$UNITY_MOD_MANAGER_DLL"

DOTNET_ROOT="$DOTNET_ROOT" \
  "$DOTNET_EXE" build "$ADOMEJI_PROJECT_ROOT/Adomeji.Bootstrap/Adomeji.Bootstrap.csproj" \
    --configuration "$configuration" \
    -p:OutputPath="$ADOMEJI_BOOTSTRAP_BUILD_OUTPUT/" \
    -p:AdofaiManaged="$ADOFAI_MANAGED" \
    -p:UnityModManagerDll="$UNITY_MOD_MANAGER_DLL"

DOTNET_ROOT="$DOTNET_ROOT" \
  "$DOTNET_EXE" build "$ADOMEJI_PROJECT_ROOT/Adomeji.UpdateEngine/Adomeji.UpdateEngine.csproj" \
    --configuration "$configuration" \
    -p:OutputPath="$ADOMEJI_UPDATE_ENGINE_BUILD_OUTPUT/" \
    -p:AdofaiManaged="$ADOFAI_MANAGED" \
    -p:UnityModManagerDll="$UNITY_MOD_MANAGER_DLL"
