#!/usr/bin/env bash
# Capture a Play Store screenshot from the connected Android device.
#
# Usage:
#   ./capture.sh <slot> <name>
# Example:
#   ./capture.sh 1 home-active-fast
#
# Output: playstore/screenshots/phone/<slot>-<name>.png
# Requires: adb in PATH, one Fast Track device connected (-s overridable via $DEVICE).

set -euo pipefail

SLOT="${1:?slot number required (e.g. 1)}"
NAME="${2:?short slug required (e.g. home-active-fast)}"
DEVICE="${DEVICE:-7d270f68}"

OUT_DIR="$(cd "$(dirname "$0")" && pwd)/screenshots/phone"
mkdir -p "$OUT_DIR"
OUT_FILE="$OUT_DIR/$(printf '%02d' "$SLOT")-${NAME}.png"

# screencap -p emits PNG bytes to stdout — pipe straight to disk.
# Suppress the spurious "no carriage return translation" warning on some adb builds.
adb -s "$DEVICE" exec-out screencap -p > "$OUT_FILE"

# Verify it actually wrote a PNG (some adb / bash combos drop bytes if the pipe stalls).
if [[ ! -s "$OUT_FILE" ]] || ! head -c 8 "$OUT_FILE" | xxd | grep -q "8950 4e47"; then
    echo "ERROR: $OUT_FILE looks malformed — re-run." >&2
    exit 1
fi

WIDTH_X_HEIGHT=$(file "$OUT_FILE" | sed 's/.*\([0-9]\{3,\}\) x \([0-9]\{3,\}\).*/\1x\2/' | head -c 20)
printf '  ✔ %s (%s)\n' "$(basename "$OUT_FILE")" "$WIDTH_X_HEIGHT"
