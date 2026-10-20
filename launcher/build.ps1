# Build the BLACK PC launcher.
#   .\build.ps1                 build only
#   .\build.ps1 -Deploy         build and copy blacklauncher.exe into the port folder
param(
    [string]$PortDir = 'D:\black-pc',
    [switch]$Deploy
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

dotnet build (Join-Path $root 'BlackLauncher.csproj') -c Release -v m
if ($LASTEXITCODE -ne 0) { throw "build failed" }

$exe = Join-Path $root 'bin\Release\net48\blacklauncher.exe'
Write-Host "built $exe"

if ($Deploy) {
    Copy-Item $exe (Join-Path $PortDir 'blacklauncher.exe') -Force
    Write-Host "deployed to $PortDir\blacklauncher.exe"
}
