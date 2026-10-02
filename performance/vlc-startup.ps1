param(
 [Parameter(Mandatory=$true)][string]$NativeDirectory,
 [Parameter(Mandatory=$true)][string]$RunDirectory,
 [Parameter(Mandatory=$true)][string]$ProductionCommit,
 [int]$Runs=5,
 [string]$FirstSample
)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$native=(Resolve-Path -LiteralPath $NativeDirectory).Path
$destination=(Resolve-Path -LiteralPath $RunDirectory).Path
foreach($path in @($native,$destination)){
 if(-not $path.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Native benchmarks require workspace-owned inputs and outputs.'}
}
if($Runs -lt 1 -or $ProductionCommit -notmatch '^[a-fA-F0-9]{40}$'){throw 'Invalid benchmark arguments'}
$benchmark=(Resolve-Path -LiteralPath (Join-Path $repo 'tests/VPlayer.Performance/bin/x64/Release/netcoreapp3.1/VPlayer.Performance.dll')).Path
$cache=Test-Path -LiteralPath (Join-Path $native 'plugins/plugins.dat')
for($i=0;$i -lt $Runs;$i++){
 $output=Join-Path $destination ('native-'+$i+'.json')
 if(Test-Path -LiteralPath $output){throw 'Native samples are immutable; use a new output directory.'}
 if($i -eq 0 -and $FirstSample){Copy-Item -LiteralPath $FirstSample -Destination $output}
 else{
  $process=Start-Process -FilePath 'dotnet' -ArgumentList @(('"'+$benchmark+'"'),'vlc-startup',('"'+$native+'"'),('"'+$output+'"'),$ProductionCommit) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $destination ('native-'+$i+'.stdout.log')) -RedirectStandardError (Join-Path $destination ('native-'+$i+'.stderr.log'))
  if(-not $process.WaitForExit(120000)){
   Stop-Process -Id $process.Id -Force
   if(-not $process.WaitForExit(10000)){throw 'Owned native benchmark did not stop'}
   throw 'Native initialization timed out; retained logs'
  }
  if($process.ExitCode -ne 0){throw ('Native benchmark failed; see '+$destination)}
 }
 $sample=Get-Content -LiteralPath $output -Raw|ConvertFrom-Json
 if($sample.Schema -ne 'vlc-startup-v1' -or $sample.Commit -ne $ProductionCommit -or $sample.NativeDirectory -ne $native -or $sample.CachePresent -ne $cache -or $sample.DecodedFrames -ne 96000 -or $sample.DecodeFailures -ne 0 -or $sample.DurationMilliseconds -ne 2000){throw 'Invalid native sample'}
 Write-Output ('Native '+$i+': '+[math]::Round($sample.TotalInitializationMilliseconds)+' ms; decode verified.')
}