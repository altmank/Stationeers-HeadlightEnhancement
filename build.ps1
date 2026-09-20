<#
.SYNOPSIS
  Build HeadlightEnhancementMod and stage the Steam Workshop package.

.DESCRIPTION
  Produces .\package\ containing the About folder and the built DLL. With -Deploy, also
  copies it into the local mods folder so the in-game mod list picks it up.

.EXAMPLE
  .\build.ps1 -Deploy
#>
[CmdletBinding()]
param(
    [string]$GameDir = $env:STATIONEERS_DIR,
    [switch]$Deploy,
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

if (-not $GameDir) {
    $GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Stationeers'
}
if (-not (Test-Path (Join-Path $GameDir 'rocketstation_Data\Managed\Assembly-CSharp.dll'))) {
    throw "Stationeers not found at '$GameDir'. Pass -GameDir or set STATIONEERS_DIR."
}

Write-Host "Building against $GameDir"
dotnet build (Join-Path $root 'src\HeadlightEnhancementMod.csproj') `
    -c $Configuration -p:GameDir="$GameDir" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$dll = Join-Path $root "src\bin\$Configuration\HeadlightEnhancementMod.dll"
if (-not (Test-Path $dll)) { throw "Build produced no DLL at $dll" }

$package = Join-Path $root 'package'
if (Test-Path $package) { Remove-Item $package -Recurse -Force }
New-Item -ItemType Directory -Path $package | Out-Null
Copy-Item (Join-Path $root 'About') $package -Recurse
Copy-Item $dll $package

# Steam rejects workshop previews over 1 MB.
$thumb = Get-Item (Join-Path $package 'About\thumb.png')
if ($thumb.Length -gt 1MB) {
    Write-Warning "About\thumb.png is $([math]::Round($thumb.Length/1MB,2)) MB. Steam caps previews at 1 MB and the game will silently fall back to a blank image."
}

Write-Host "Staged $package"

if ($Deploy) {
    $mods = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'My Games\Stationeers\mods\HeadlightEnhancement'
    if (Test-Path $mods) { Remove-Item $mods -Recurse -Force }
    New-Item -ItemType Directory -Path $mods -Force | Out-Null
    Copy-Item "$package\*" $mods -Recurse
    Write-Host "Deployed to $mods"
    Write-Host 'Launch Stationeers; the mod appears in the in-game mods list as a local mod.'
}
