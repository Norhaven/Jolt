$ErrorActionPreference = 'Stop'

# README links are relative to this folder (e.g. images/foo.png) so that they work on GitHub. vsce rewrites relative
# links against the repository root by default, so point it at this folder instead for the Marketplace page.
# Images are served from GitHub (not the VSIX), so they must be pushed to the default branch before publishing.
$baseUrl = 'https://github.com/Norhaven/Jolt/raw/HEAD/IDE/Extensions/VSCode/jolt'

$manifest = Get-Content (Join-Path $PSScriptRoot 'package.json') -Raw | ConvertFrom-Json
$packagePath = Join-Path $PSScriptRoot "$($manifest.name)-$($manifest.version).vsix"

vsce package --baseImagesUrl $baseUrl --baseContentUrl $baseUrl --out $packagePath

Write-Host "Package: $($manifest.name)"
Write-Host "Version: $($manifest.version)"
Write-Host "Path: $packagePath"
