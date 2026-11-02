param(
	[int]$Major,
	[int]$Minor,
	[int]$Revision
)

$manifestPath = Join-Path $PSScriptRoot "package.json"

function Get-VersionFromFile {
	param([string]$FilePath)

	$manifest = Get-Content $FilePath -Raw | ConvertFrom-Json
	return $manifest.version
}

function Set-VersionInFile {
	param([string]$NewVersion)

	# npm keeps the existing formatting and updates package-lock.json to match. It doesn't create a git commit or tag here.

	Push-Location $PSScriptRoot

	try {
		npm version $NewVersion --no-git-tag-version --allow-same-version | Out-Null

		if ($LASTEXITCODE -ne 0) {
			throw "npm version failed with exit code $LASTEXITCODE"
		}
	} finally {
		Pop-Location
	}
}

function Increment-Revision {
	param([string]$Version)

	$parts = $Version -split '\.'
	if ($parts.Count -ne 3) {
		throw "Invalid version format: $Version. Expected format: major.minor.revision"
	}

	$major = [int]$parts[0]
	$minor = [int]$parts[1]
	$revision = [int]$parts[2]

	$revision++

	return "$major.$minor.$revision"
}

# Main script logic
if (-not (Test-Path $manifestPath)) {
	Write-Error "Extension manifest not found: $manifestPath"
	exit 1
}

if ($PSBoundParameters.Keys.Count -eq 0) {
	# No parameters provided: increment revision
	Write-Host "Incrementing revision version of the extension..."

	$currentVersion = Get-VersionFromFile $manifestPath
	$newVersion = Increment-Revision $currentVersion
} elseif ($PSBoundParameters.Keys.Count -eq 3) {
	# All three parameters provided: set specific version
	$newVersion = "$Major.$Minor.$Revision"
	Write-Host "Setting the extension to version: $newVersion"

	$currentVersion = Get-VersionFromFile $manifestPath
} else {
	# Partial parameters: error
	Write-Error "Invalid parameter combination. Either provide no parameters to increment revision, or provide all three: -Major, -Minor, -Revision"
	exit 1
}

Set-VersionInFile $newVersion

Write-Host "Updated package.json: $currentVersion -> $newVersion"
