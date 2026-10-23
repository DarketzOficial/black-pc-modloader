#!/usr/bin/env bash
# BLACK PC ModLoader - native Linux launcher
set -e
cd "$(dirname "$0")"
exec python3 blacklauncher-linux.py "$@"
