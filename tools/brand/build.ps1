# Rebuilds every VeriPresenX brand asset from brand/veripresenx-master.jpg.
#
#   powershell -ExecutionPolicy Bypass -File tools\brand\build.ps1
#
# Offline, deterministic, no npm or ImageMagick. Requires Windows PowerShell 5.1
# (.NET Framework System.Drawing). Overwrites everything in brand/.
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.Drawing

$root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

$sources = @(
  (Join-Path $PSScriptRoot "BrandBuild.cs"),
  (Join-Path $PSScriptRoot "BrandBuild.Keying.cs"),
  (Join-Path $PSScriptRoot "BrandBuild.Finish.cs"),
  (Join-Path $PSScriptRoot "BrandBuild.Sheet.cs")
)

foreach ($s in $sources) {
  if (-not (Test-Path $s)) { throw "Missing source file: $s" }
}

Add-Type -Path $sources -ReferencedAssemblies @("System", "System.Core", "System.Drawing")

[BrandBuild]::Run($root)
