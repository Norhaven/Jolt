param(
	[int]$Major,
	[int]$Minor,
	[int]$Revision
)

$projectRoot = $PSScriptRoot

$projects = @(
	Join-Path $projectRoot "Jolt" "Jolt.csproj"
	Join-Path $projectRoot "Jolt.Json.Newtonsoft" "Jolt.Json.Newtonsoft.csproj"
	Join-Path $projectRoot "Jolt.Json.DotNet" "Jolt.Json.DotNet.csproj"
)

function Get-VersionFromFile {
	param([string]$FilePath)

	[xml]$xml = Get-Content $FilePath
	$propertyGroup = $xml.Project.PropertyGroup | Where-Object { $_.Version }
	$version = $propertyGroup.Version
	return $version
}

function Set-VersionInFile {
	param(
		[string]$FilePath,
		[string]$NewVersion
	)

	[xml]$xml = Get-Content $FilePath
	$propertyGroup = $xml.Project.PropertyGroup | Where-Object { $_.Version }

	if ($propertyGroup) {
		$propertyGroup.Version = $NewVersion
		$xml.Save($FilePath)
	} else {
		throw "No PropertyGroup with Version tag found in $FilePath"
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
if ($PSBoundParameters.Keys.Count -eq 0) {
	# No parameters provided: increment revision in all projects
	Write-Host "Incrementing revision version in all projects..."

	foreach ($project in $projects) {
		if (-not (Test-Path $project)) {
			Write-Warning "Project file not found: $project"
			continue
		}

		$currentVersion = Get-VersionFromFile $project
		$newVersion = Increment-Revision $currentVersion
		Set-VersionInFile $project $newVersion

		Write-Host "Updated $([System.IO.Path]::GetFileName($project)): $currentVersion -> $newVersion"
	}
} elseif ($PSBoundParameters.Keys.Count -eq 3) {
	# All three parameters provided: set specific version
	$newVersion = "$Major.$Minor.$Revision"
	Write-Host "Setting all projects to version: $newVersion"

	foreach ($project in $projects) {
		if (-not (Test-Path $project)) {
			Write-Warning "Project file not found: $project"
			continue
		}

		$currentVersion = Get-VersionFromFile $project
		Set-VersionInFile $project $newVersion

		Write-Host "Updated $([System.IO.Path]::GetFileName($project)): $currentVersion -> $newVersion"
	}
} else {
	# Partial parameters: error
	Write-Error "Invalid parameter combination. Either provide no parameters to increment revision, or provide all three: -Major, -Minor, -Revision"
	exit 1
}