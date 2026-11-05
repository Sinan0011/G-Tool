# Builds and signs a G-Tool release, then publishes it to GitHub if the gh tool is installed.
# Usage: powershell -ExecutionPolicy Bypass -File scripts\release-g-tool.ps1 -Version 1.0.1
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [string]$Key = "$env:USERPROFILE\.g-tool\update-signing.key",
    [switch]$BuildOnly
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$output = Join-Path $root "build\release\$Version"
$app = Join-Path $output 'G-Tool'
if (-not (Test-Path $Key)) { throw "Signing key not found: $Key" }
if (Test-Path $output) { Remove-Item -Recurse -Force $output -Confirm:$false }

$dotnet = 'C:\Program Files\dotnet\dotnet.exe'
& $dotnet publish (Join-Path $root 'windows\GTool.Windows') -c Release "-p:Version=$Version" -o $app
if ($LASTEXITCODE -ne 0) { throw 'The app did not build.' }
& $dotnet run --project (Join-Path $root 'windows\G-Tool.ReleaseTool') -c Release -- release $app $Key $Version $output
if ($LASTEXITCODE -ne 0) { throw 'The release could not be signed.' }

$archive = Join-Path $output 'G-Tool-Windows-x64.zip'
$manifest = Join-Path $output 'windows-update.json'
Write-Host "Signed release files are in $output"
if ($BuildOnly) { return }

if (Get-Command gh -ErrorAction SilentlyContinue) {
    & gh release create "v$Version" $archive $manifest --title "G-Tool $Version" --generate-notes
    if ($LASTEXITCODE -ne 0) { throw 'GitHub did not accept the release.' }
    Write-Host "Published v$Version. Running copies of G-Tool will see it within an hour."
}
else {
    Write-Host "gh is not installed. On GitHub create a release tagged v$Version and attach these two files:"
    Write-Host "  $archive"
    Write-Host "  $manifest"
}
