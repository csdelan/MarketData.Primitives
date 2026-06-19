param(
	[string]$BuildSharePath = "\\Bart\BuildShare",
	[string]$DeploymentDirectoryName = "MarketWorkers",
	[string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")
$projectPath = Join-Path $repoRoot "src\MarketData.MarketWorkers\MarketData.MarketWorkers.csproj"
$publishPath = Join-Path $repoRoot "artifacts\publish\MarketWorkers"
$targetPath = Join-Path $BuildSharePath $DeploymentDirectoryName

if (-not (Test-Path -LiteralPath $projectPath)) {
	throw "Project not found: $projectPath"
}

if (Test-Path -LiteralPath $publishPath) {
	Remove-Item -LiteralPath $publishPath -Recurse -Force
}

New-Item -ItemType Directory -Path $publishPath -Force | Out-Null
New-Item -ItemType Directory -Path $targetPath -Force | Out-Null

Push-Location $repoRoot
try {
	dotnet restore $projectPath
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet restore failed for $projectPath"
	}

	dotnet publish $projectPath --configuration $Configuration --output $publishPath --no-restore /p:UseAppHost=true
	if ($LASTEXITCODE -ne 0) {
		throw "dotnet publish failed for $projectPath"
	}

	$publishedItems = Get-ChildItem -LiteralPath $publishPath -Force
	if (-not $publishedItems) {
		throw "dotnet publish completed but no files were created in $publishPath"
	}

	Get-ChildItem -LiteralPath $targetPath -Force | Remove-Item -Recurse -Force
	Copy-Item -Path (Join-Path $publishPath "*") -Destination $targetPath -Recurse -Force

	Write-Host "Deployed MarketData.MarketWorkers to $targetPath"
}
finally {
	Pop-Location
}
