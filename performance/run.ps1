param(
  [ValidateSet('baseline','optimized')][string]$Mode='baseline',
  [string]$FixtureDirectory='artifacts/performance/library-worst-case',
  [int]$StartupRuns=7,
  [switch]$Visible
)
$ErrorActionPreference='Stop'
$repository=Split-Path -Parent $PSScriptRoot
Push-Location $repository
try {
  $fixture=(Resolve-Path -LiteralPath $FixtureDirectory).Path
  $commit=(& git rev-parse HEAD).Trim()
  $runDirectory=Join-Path $repository ('artifacts/performance/runs/'+$Mode+'-'+$commit.Substring(0,8)+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
  New-Item -ItemType Directory -Path $runDirectory | Out-Null
  & dotnet build tests/VPlayer.Performance/VPlayer.Performance.csproj -c Debug -p:Platform=x64 -v quiet *> (Join-Path $runDirectory 'build-runner.log')
  if($LASTEXITCODE -ne 0) {throw 'Performance runner build failed.'}
  & dotnet build VPlayer/VPlayer.csproj -c Debug -p:Platform=x64 -v quiet *> (Join-Path $runDirectory 'build-app.log')
  if($LASTEXITCODE -ne 0) {throw 'Application build failed.'}
  # Run no builds/tests concurrently with measurement.
  & dotnet tests/VPlayer.Performance/bin/x64/Debug/netcoreapp3.1/VPlayer.Performance.dll run $fixture (Join-Path $runDirectory 'features.json') $commit
  if($LASTEXITCODE -ne 0) {throw 'Feature benchmarks failed.'}
  $application=(Resolve-Path 'VPlayer/bin/x64/Debug/netcoreapp3.1/VPlayer.exe').Path
  & (Join-Path $PSScriptRoot 'startup.ps1') -FixtureDirectory $fixture -RunDirectory $runDirectory -Application $application -Commit $commit -Runs $StartupRuns -Visible:$Visible
  Write-Output ('Saved '+$runDirectory)
} finally {Pop-Location}
