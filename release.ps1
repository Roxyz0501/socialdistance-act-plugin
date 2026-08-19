param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version,
    [string]$Configuration = 'Release',
    [string]$ActPath = 'C:\Program Files (x86)\Advanced Combat Tracker\Advanced Combat Tracker.exe'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
& dotnet build (Join-Path $root 'AetherRange\AetherRange.csproj') -c $Configuration "-p:ActPath=$ActPath" --configfile (Join-Path $root 'NuGet.Config')
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

$dll = Join-Path $root "AetherRange\bin\$Configuration\net48\SocialDistance.dll"
$actual = [Reflection.AssemblyName]::GetAssemblyName($dll).Version
$actualSemVer = "$($actual.Major).$($actual.Minor).$($actual.Build)"
if ($actualSemVer -ne $Version) { throw "DLL version $actualSemVer does not match requested release $Version." }

$releaseDir = Join-Path $root 'release'
New-Item -ItemType Directory -Force -Path $releaseDir | Out-Null
$staging = Join-Path $releaseDir "SocialDistance-v$Version"
New-Item -ItemType Directory -Force -Path $staging | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $staging 'SocialDistance.dll') -Force
Copy-Item -LiteralPath (Join-Path $root 'README.md') -Destination (Join-Path $staging 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $root 'AetherRange\Assets\Jobs\XIVAPI-LICENSE.txt') -Destination (Join-Path $staging 'XIVAPI-LICENSE.txt') -Force

$zip = Join-Path $releaseDir "SocialDistance-v$Version.zip"
Compress-Archive -Path (Join-Path $staging '*') -DestinationPath $zip -Force
$hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $zip).Hash.ToLowerInvariant()
$manifest = Join-Path $releaseDir "SocialDistance-v$Version.zip.sha256"
Set-Content -LiteralPath $manifest -Value "$hash  SocialDistance-v$Version.zip" -Encoding UTF8
Write-Host "Created $zip"
Write-Host "Created $manifest"
