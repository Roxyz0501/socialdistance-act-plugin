param(
    [string]$Configuration = "Release",
    [string]$ActPath = "C:\Program Files (x86)\Advanced Combat Tracker\Advanced Combat Tracker.exe"
)

$ErrorActionPreference = "Stop"
$projectPath = Join-Path $PSScriptRoot "AetherRange\AetherRange.csproj"

if (-not (Test-Path -LiteralPath $ActPath)) {
    throw "Advanced Combat Tracker.exe was not found. Pass its path with -ActPath."
}

dotnet build $projectPath -c $Configuration -p:ActPath="$ActPath" --configfile (Join-Path $PSScriptRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) {
    throw "Build failed."
}

$updaterPath = Join-Path $PSScriptRoot "SocialDistance.Updater\SocialDistance.Updater.csproj"
dotnet build $updaterPath -c $Configuration --configfile (Join-Path $PSScriptRoot "NuGet.Config")
if ($LASTEXITCODE -ne 0) {
    throw "Update helper build failed."
}

$output = Join-Path $PSScriptRoot "AetherRange\bin\$Configuration\net48\SocialDistance.dll"
Write-Host "Built: $output"
