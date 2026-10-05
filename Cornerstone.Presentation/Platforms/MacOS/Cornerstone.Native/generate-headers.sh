#!/bin/bash
set -euo pipefail
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
IDL="$SCRIPT_DIR/../csn.idl"
OUT="$SCRIPT_DIR/inc/cornerstone-native.h"
dotnet run --project "$SCRIPT_DIR/GenerateCppHeaders/GenerateCppHeaders.csproj" -c Release -- "$IDL" "$OUT"
