#!/bin/bash
# Builds libCornerstoneNative.dylib (Xcode). Run on macOS.
# Output: Native/Build/Products/Release/libCornerstoneNative.dylib
set -euo pipefail
SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )
"$SCRIPT_DIR/generate-headers.sh"
xcodebuild \
	-project "$SCRIPT_DIR/src/OSX/Cornerstone.Native.OSX.xcodeproj" \
	-configuration Release \
	SYMROOT="$SCRIPT_DIR/../Build/Products"
echo "Built $SCRIPT_DIR/../Build/Products/Release/libCornerstoneNative.dylib"
