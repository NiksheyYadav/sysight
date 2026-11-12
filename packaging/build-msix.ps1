<#
.SYNOPSIS
  Builds Sysight and packs it into an .msix for the Microsoft Store.

.EXAMPLE
  # Local test package (default identity)
  ./packaging/build-msix.ps1

.EXAMPLE
  # Store package: copy the three values from Partner Center > Product management > Product identity
  ./packaging/build-msix.ps1 -IdentityName "12345YourName.Sysight" -Publisher "CN=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX" -PublisherDisplayName "Your Name" -Version 1.0.0.0
#>
param(
  [string]$IdentityName = "Sysight.Local",
  [string]$Publisher = "CN=SysightDev",
  [string]$PublisherDisplayName = "Sysight",
  [string]$Version = "1.0.0.0",
  [switch]$Register   # also register the unpacked layout on this PC (needs Developer Mode)
)
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$out = Join-Path $root "out"
$layout = Join-Path $out "layout"

Write-Host "1/4 Fetching MSIX tools"
dotnet restore (Join-Path $PSScriptRoot "tools/tools.csproj") -v q | Out-Null
$makeappx = Get-ChildItem "$env:USERPROFILE\.nuget\packages\microsoft.windows.sdk.buildtools" -Recurse -Filter makeappx.exe |
  Where-Object FullName -match '\\x64\\' | Select-Object -First 1 -ExpandProperty FullName
if (-not $makeappx) { throw "makeappx.exe not found" }

Write-Host "2/4 Publishing self-contained build"
if (Test-Path $layout) { Remove-Item $layout -Recurse -Force }
$appVersion = ($Version -split '\.')[0..2] -join '.'
dotnet publish (Join-Path $root "src/Sysight/Sysight.csproj") -c Release -o $layout -p:Version=$appVersion -nologo -v q
if ($LASTEXITCODE) { throw "dotnet publish failed" }

Write-Host "3/4 Writing manifest and assets"
Copy-Item (Join-Path $PSScriptRoot "Assets") (Join-Path $layout "Assets") -Recurse
(Get-Content (Join-Path $PSScriptRoot "AppxManifest.template.xml") -Raw).
  Replace("{{IDENTITY_NAME}}", $IdentityName).Replace("{{PUBLISHER}}", $Publisher).
  Replace("{{PUBLISHER_DISPLAY_NAME}}", [Security.SecurityElement]::Escape($PublisherDisplayName)).Replace("{{VERSION}}", $Version) |
  Set-Content (Join-Path $layout "AppxManifest.xml") -Encoding utf8

Write-Host "4/4 Packing"
$msix = Join-Path $out "Sysight_$($Version)_x64.msix"
& $makeappx pack /d $layout /p $msix /o /h SHA256 | Out-Null
if ($LASTEXITCODE) { throw "makeappx failed" }
Write-Host ("Done: {0} ({1:N1} MB)" -f $msix, ((Get-Item $msix).Length / 1MB))

if ($Register) {
  Add-AppxPackage -Register (Join-Path $layout "AppxManifest.xml") -ForceApplicationShutdown
  Write-Host "Registered on this PC. Find 'Sysight' in the Start menu."
}
