$ErrorActionPreference = 'Stop'

# Copies the Jolt.Testing JSON test files into ./tests as .jolt.json files so that the extension tests and the extension
# development host always work against the latest tests. Existing files with the same name are overwritten; files that
# only exist in ./tests (e.g. extension-specific examples) are left alone. The schema and its docs are not test files.
$sourcePath = Join-Path $PSScriptRoot '../../../../Tests/Jolt.Testing/Resources/TestFiles/JsonTests'
$destinationPath = Join-Path $PSScriptRoot 'tests'

if (-not (Test-Path $sourcePath)) {
    throw "Test file folder not found: $sourcePath"
}

New-Item -ItemType Directory -Force $destinationPath | Out-Null

$testFiles = Get-ChildItem -Path $sourcePath -File -Filter '*.json' | Where-Object { $_.Name -notlike '*.schema.json' }

foreach ($file in $testFiles) {
    $targetPath = Join-Path $destinationPath "$($file.BaseName).jolt.json"
    Copy-Item -Path $file.FullName -Destination $targetPath -Force
    Write-Host "Copied $($file.Name) -> tests/$($file.BaseName).jolt.json"
}

Write-Host "Copied $($testFiles.Count) test file(s)."
