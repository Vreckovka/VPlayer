param(
  [Parameter(Mandatory=$true)][string]$FixtureDirectory,
  [Parameter(Mandatory=$true)][string]$RunDirectory,
  [Parameter(Mandatory=$true)][string]$Commit,
  [string]$Application='tests/VPlayer.Performance/bin/x64/Release/netcoreapp3.1/VPlayer.Performance.dll'
)
$ErrorActionPreference='Stop'
if($Commit -notmatch '^[0-9a-f]{40}$'){throw 'Requires a full source commit.'}
$fixture=(Resolve-Path -LiteralPath $FixtureDirectory).Path
$destination=(Resolve-Path -LiteralPath $RunDirectory).Path
$app=(Resolve-Path -LiteralPath $Application).Path
$metadata=Get-Content (Join-Path $fixture 'fixture.json') -Raw|ConvertFrom-Json
$database=Join-Path $fixture 'VPlayerDatabase.db'
if((Get-FileHash $database -Algorithm SHA256).Hash -ne $metadata.DatabaseSha256){throw 'Fixture changed.'}
for($i=0;$i -lt 3;$i++) {
  $output=Join-Path $destination ('saved-views-'+$i+'.json')
  if(Test-Path -LiteralPath $output){throw 'Run outputs are immutable; use a new directory.'}
  & dotnet $app saved-playlist-views $fixture $output $Commit
  if($LASTEXITCODE -ne 0){throw ('Saved view benchmark failed: '+$LASTEXITCODE)}
  $record=Get-Content -LiteralPath $output -Raw|ConvertFrom-Json
  if($record.Commit -ne $Commit -or $record.FixtureSha256 -ne $metadata.DatabaseSha256 -or
    $record.Entries -ne 100000 -or @($record.Samples).Count -ne 5){throw 'Invalid benchmark evidence.'}
}
if((Get-FileHash $database -Algorithm SHA256).Hash -ne $metadata.DatabaseSha256){throw 'Fixture changed during benchmark.'}
