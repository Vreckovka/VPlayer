param(
  [Parameter(Mandatory=$true)][string]$FixtureDirectory,
  [Parameter(Mandatory=$true)][string]$RunDirectory,
  [Parameter(Mandatory=$true)][string]$Application,
  [Parameter(Mandatory=$true)][string]$Commit,
  [int]$Runs=7,
  [switch]$Visible,
  [switch]$ScrollPlaylists,
  [switch]$Statistics
)
$ErrorActionPreference='Stop'
$fixture=(Resolve-Path -LiteralPath $FixtureDirectory).Path
$destination=(Resolve-Path -LiteralPath $RunDirectory).Path
$app=(Resolve-Path -LiteralPath $Application).Path
$metadata=Get-Content -LiteralPath (Join-Path $fixture 'fixture.json') -Raw | ConvertFrom-Json
$checksum=(Get-FileHash -LiteralPath (Join-Path $fixture 'VPlayerDatabase.db') -Algorithm SHA256).Hash
if($checksum -ne $metadata.DatabaseSha256) {throw 'Performance fixture changed.'}
$environmentMetadata=@{
  FixtureSha256=$checksum;SoundItems=$metadata.SoundItems;Playlists=$metadata.Playlists
  Configuration='Release';ProcessorCount=[Environment]::ProcessorCount;OS=[Environment]::OSVersion.VersionString
}
$originalValues=@{}
foreach($name in @('VPLAYER_BENCHMARK_DIRECTORY','VPLAYER_PERFORMANCE_RUN_FILE','VPLAYER_PERFORMANCE_COMMIT','VPLAYER_PERFORMANCE_EXIT_AFTER_RENDER','VPLAYER_PERFORMANCE_WAIT_FOR_LIBRARY','VPLAYER_PERFORMANCE_SCROLL_PLAYLISTS','VPLAYER_PERFORMANCE_STATISTICS')) {
  $originalValues[$name]=[Environment]::GetEnvironmentVariable($name,'Process')
}
try {
  for($i=0;$i -lt $Runs;$i++) {
    $result=Join-Path $destination ('startup-'+$i+'.json')
    if(Test-Path -LiteralPath $result) {throw 'Startup outputs are immutable; use a new run directory.'}
    $profile=Join-Path $destination ('startup-profile-'+$i)
    New-Item -ItemType Directory -Path $profile | Out-Null
    Copy-Item -LiteralPath (Join-Path $fixture 'VPlayerDatabase.db') -Destination (Join-Path $profile 'VPlayerDatabase.db')
    $env:VPLAYER_BENCHMARK_DIRECTORY=$profile
    $env:VPLAYER_PERFORMANCE_RUN_FILE=$result
    $env:VPLAYER_PERFORMANCE_COMMIT=$Commit
    $env:VPLAYER_PERFORMANCE_EXIT_AFTER_RENDER='1'
    $env:VPLAYER_PERFORMANCE_WAIT_FOR_LIBRARY='1'
    $env:VPLAYER_PERFORMANCE_SCROLL_PLAYLISTS=$(if($ScrollPlaylists){'1'}else{'0'})
    $env:VPLAYER_PERFORMANCE_STATISTICS=$(if($Statistics){'1'}else{'0'})
    $launch=@{
      FilePath=$app;WorkingDirectory=(Split-Path -Parent $app);PassThru=$true
      WindowStyle=$(if($Visible){'Normal'}else{'Hidden'})
      RedirectStandardOutput=(Join-Path $destination ('startup-'+$i+'.stdout.log'))
      RedirectStandardError=(Join-Path $destination ('startup-'+$i+'.stderr.log'))
    }
    $process=Start-Process @launch
    if(!$process.WaitForExit(60000)) {
      Stop-Process -Id $process.Id -Force
      $record=@{Status='Timeout';Commit=$Commit;Phases=@();TimeoutSeconds=60}
      if(Test-Path -LiteralPath $result) {
        $record=Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
        $record.Status='Timeout'
        $record | Add-Member -NotePropertyName TimeoutSeconds -NotePropertyValue 60 -Force
      }
    } elseif(Test-Path -LiteralPath $result) {
      $record=Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
    } else {
      $record=@{Status='ExitedWithoutRender';ExitCode=$process.ExitCode;Commit=$Commit;Phases=@()}
    }
    if($record -is [Collections.IDictionary]) {$record['WindowMode']=$(if($Visible){'Visible'}else{'Hidden'})}
    else {$record | Add-Member -NotePropertyName WindowMode -NotePropertyValue $(if($Visible){'Visible'}else{'Hidden'}) -Force}
    foreach($name in $environmentMetadata.Keys) {
      if($record -is [Collections.IDictionary]) {$record[$name]=$environmentMetadata[$name]}
      else {$record | Add-Member -NotePropertyName $name -NotePropertyValue $environmentMetadata[$name] -Force}
    }
    $record | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $result
    Write-Output ('Startup '+$i+': '+$record.Status)
  }
} finally {
  foreach($name in $originalValues.Keys) {[Environment]::SetEnvironmentVariable($name,$originalValues[$name],'Process')}
}
