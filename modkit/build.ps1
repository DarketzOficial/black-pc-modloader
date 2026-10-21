# SPDX-License-Identifier: GPL-2.0-or-later
# compila o ModKit e deixa o executável em modkit\dist
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
dotnet build (Join-Path $root "BlackModKit.csproj") --configuration Release
$bin = Join-Path $root "bin\Release\net48"
$dist = Join-Path $root "dist"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Path $dist | Out-Null
Copy-Item (Join-Path $bin "blackmodkit.exe") $dist
if (Test-Path (Join-Path $bin "blackmodkit.exe.config")) {
    Copy-Item (Join-Path $bin "blackmodkit.exe.config") $dist
}
Write-Host "ModKit em $dist"
