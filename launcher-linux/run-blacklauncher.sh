#!/usr/bin/env bash
# BLACK PC ModLoader - native Linux launcher (Avalonia, same UI as the Windows launcher)
# The launcher is native; the game and blackrt.dll are Win32 and run under Wine.
set -e
here="$(cd "$(dirname "$0")" && pwd)"
cd "$here"
exec "$here/launcher-linux/blacklauncher" --port "$here" "$@"
