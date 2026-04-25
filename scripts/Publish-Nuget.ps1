param(
    [string]$NuGetPath = "\\BART\MyNuget",
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptRoot "..")
$solutionPath = Join-Path $repoRoot "MarketData.Primitives.sln"
$artifactsPath = Join-Path $repoRoot "artifacts\nuget"

$projects = @(
    Join-Path $repoRoot "src\MarketData.Primitives\MarketData.Primitives.csproj"
    Join-Path $repoRoot "src\MarketData.Application\MarketData.Application.csproj"
    Join-Path $repoRoot "src\MarketData.Infrastructure\MarketData.Infrastructure.csproj"
)

if (-not (Test-Path -LiteralPath $solutionPath)) {
    throw "Solution not found: $solutionPath"
}

foreach ($project in $projects) {
    if (-not (Test-Path -LiteralPath $project)) {
        throw "Project not found: $project"
    }
}

$isFileSource = [System.IO.Path]::IsPathRooted($NuGetPath) -or $NuGetPath.StartsWith("\\")

if ($isFileSource -and -not (Test-Path -LiteralPath $NuGetPath)) {
    New-Item -ItemType Directory -Path $NuGetPath | Out-Null
}

if (Test-Path -LiteralPath $artifactsPath) {
    Remove-Item -LiteralPath $artifactsPath -Recurse -Force
}

New-Item -ItemType Directory -Path $artifactsPath | Out-Null

Push-Location $repoRoot
try {
    dotnet restore $solutionPath

    foreach ($project in $projects) {
        dotnet build $project --configuration $Configuration --no-restore
    }

    foreach ($project in $projects) {
        dotnet pack $project --configuration $Configuration --no-build --output $artifactsPath
    }

    $packages = Get-ChildItem -LiteralPath $artifactsPath -Filter "*.nupkg" -File |
        Where-Object { $_.Name -notlike "*.symbols.nupkg" }

    if (-not $packages) {
        throw "No NuGet packages were created in $artifactsPath"
    }

    foreach ($package in $packages) {
        if ($isFileSource) {
            $existingPackage = Get-ChildItem -LiteralPath $NuGetPath -Filter $package.Name -File -Recurse -ErrorAction SilentlyContinue |
                Select-Object -First 1
            $targetPath = Join-Path $NuGetPath $package.Name

            if ($existingPackage) {
                $response = Read-Host "Package already exists in ${NuGetPath}: $($package.Name). Overwrite? [y/N]"

                if ($response -notin @("y", "Y", "yes", "Yes", "YES")) {
                    Write-Host "Skipping $($package.Name)"
                    continue
                }

                $targetPath = $existingPackage.FullName
                Remove-Item -LiteralPath $existingPackage.FullName -Force
            }

            $targetDirectory = Split-Path -Parent $targetPath
            if (-not (Test-Path -LiteralPath $targetDirectory)) {
                New-Item -ItemType Directory -Path $targetDirectory | Out-Null
            }

            Copy-Item -LiteralPath $package.FullName -Destination $targetPath -Force
            Write-Host "Published $($package.Name) to $targetPath"
        }
        else {
            dotnet nuget push $package.FullName --source $NuGetPath --skip-duplicate
        }
    }
}
finally {
    Pop-Location
}
