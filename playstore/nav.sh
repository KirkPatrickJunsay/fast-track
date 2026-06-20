#!/usr/bin/env bash
# Drive the Fast Track bottom tab bar from the terminal.
#
# Usage:
#   ./nav.sh home       # or history / protocols / insights / learn / settings
#
# Coordinates are calibrated for the Xiaomi Redmi Note 13 Pro 5G (1080×2400,
# 440 dpi, 6 visible tabs at the bottom). Other devices will need re-measurement.

set -euo pipefail

TAB="${1:?tab name required (home | history | protocols | insights | learn | settings)}"
DEVICE="${DEVICE:-7d270f68}"

# 6 tabs split a 1080-wide screen into 180px columns.
# Centres: 90, 270, 450, 630, 810, 990.
# Tab bar y-centre lands at ~2275 on this device (above the 3-button nav gesture bar).
declare -A X=(
  [home]=90
  [history]=270
  [protocols]=450
  [insights]=630
  [learn]=810
  [settings]=990
)
Y=2275

if [[ -z "${X[$TAB]:-}" ]]; then
    echo "Unknown tab: $TAB" >&2
    echo "Valid: home history protocols insights learn settings" >&2
    exit 1
fi

adb -s "$DEVICE" shell input tap "${X[$TAB]}" "$Y"
# A small pause so the tab animation settles before any follow-up screencap.
sleep 0.6
