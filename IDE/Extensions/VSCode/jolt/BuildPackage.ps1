# Packages the extension, and publishes that exact package to the Visual Studio Marketplace with -Publish and to
# Open VSX (used by VSCodium, Cursor, Theia and other editors) with -OpenVsx. Publishing to Open VSX needs an access
# token from open-vsx.org in the OVSX_PAT environment variable.
param(
    [switch]$Publish,
    [switch]$OpenVsx
)

$ErrorActionPreference = 'Stop'

# Checked before packaging, so that a missing token doesn't leave the Marketplace published but Open VSX not.
if ($OpenVsx -and -not $env:OVSX_PAT) {
    throw 'Publishing to Open VSX needs an access token from https://open-vsx.org/user-settings/tokens in the OVSX_PAT environment variable.'
}

# README links are relative to this folder (e.g. images/foo.png) so that they work on GitHub. vsce rewrites relative
# links against the repository root by default, so point it at this folder instead for the Marketplace page.
# Images are served from GitHub (not the VSIX), so they must be pushed to the default branch before publishing.
$baseUrl = 'https://github.com/Norhaven/Jolt/raw/HEAD/IDE/Extensions/VSCode/jolt'

$manifest = Get-Content (Join-Path $PSScriptRoot 'package.json') -Raw | ConvertFrom-Json
$packagePath = Join-Path $PSScriptRoot "$($manifest.name)-$($manifest.version).vsix"

vsce package --baseImagesUrl $baseUrl --baseContentUrl $baseUrl --out $packagePath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if ($Publish) {
    # Publish the package built above rather than letting vsce repackage, which would drop the base URL options.
    vsce publish --packagePath $packagePath
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

if ($OpenVsx) {
    # ovsx is a dev dependency of this folder, and reads the token from OVSX_PAT.
    Push-Location $PSScriptRoot

    try {
        npx ovsx publish $packagePath
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    finally {
        Pop-Location
    }
}
