param(
  [Parameter(Mandatory=$true)][string]$FixtureDirectory,
  [Parameter(Mandatory=$true)][string]$RunDirectory,
  [Parameter(Mandatory=$true)][string]$Application,
  [Parameter(Mandatory=$true)][string]$Commit,
  [int]$Runs=7,
  [switch]$Visible,
  [switch]$ScrollPlaylists,
  [switch]$Statistics,
  [switch]$StatisticsReloads,
  [switch]$MusicPlaylist,
  [switch]$MusicPlaylistSave,
  [switch]$MusicPlaylistClear,
  [switch]$PreparePlaylistQuery,
  [switch]$CpuProfile,
  [string]$TraceTool
)
$ErrorActionPreference='Stop'
if($MusicPlaylistSave -and $MusicPlaylistClear){throw 'Save and clear require separate launches.'}
if($MusicPlaylistSave -or $MusicPlaylistClear){$MusicPlaylist=$true}
if($MusicPlaylist -and (!$Visible -or $Statistics -or $StatisticsReloads)){throw 'Music playlist benchmarks require visible, separate launches.'}
$fixture=(Resolve-Path -LiteralPath $FixtureDirectory).Path
$destination=(Resolve-Path -LiteralPath $RunDirectory).Path
$app=(Resolve-Path -LiteralPath $Application).Path
$traceExecutable=if($TraceTool){(Resolve-Path -LiteralPath $TraceTool).Path}else{$null}
$metadata=Get-Content -LiteralPath (Join-Path $fixture 'fixture.json') -Raw | ConvertFrom-Json
$checksum=(Get-FileHash -LiteralPath (Join-Path $fixture 'VPlayerDatabase.db') -Algorithm SHA256).Hash
if($checksum -ne $metadata.DatabaseSha256) {throw 'Performance fixture changed.'}
$timeoutSeconds=if($MusicPlaylistSave -or $MusicPlaylistClear){180}else{60}
$environmentMetadata=@{
  FixtureSha256=$checksum;SoundItems=$metadata.SoundItems;Playlists=$metadata.Playlists
  PlaylistQueryPreparation=[bool]$PreparePlaylistQuery
  MusicPlaylistEntries=$(if($MusicPlaylist){100000}else{0})
  MusicPlaylistSave=[bool]$MusicPlaylistSave;MusicPlaylistClear=[bool]$MusicPlaylistClear
  ProcessTimeoutSeconds=$timeoutSeconds
  Configuration='Release';ProcessorCount=[Environment]::ProcessorCount;OS=[Environment]::OSVersion.VersionString
}
$originalValues=@{}
foreach($name in @('VPLAYER_BENCHMARK_DIRECTORY','VPLAYER_PERFORMANCE_RUN_FILE','VPLAYER_PERFORMANCE_COMMIT','VPLAYER_PERFORMANCE_EXIT_AFTER_RENDER','VPLAYER_PERFORMANCE_WAIT_FOR_LIBRARY','VPLAYER_PERFORMANCE_SCROLL_PLAYLISTS','VPLAYER_PERFORMANCE_STATISTICS','VPLAYER_PERFORMANCE_STATISTICS_RELOADS','VPLAYER_PERFORMANCE_MUSIC_PLAYLIST','VPLAYER_PERFORMANCE_MUSIC_PLAYLIST_SAVE','VPLAYER_PERFORMANCE_MUSIC_PLAYLIST_CLEAR','VPLAYER_PERFORMANCE_PREPARE_PLAYLIST_QUERY','VPLAYER_PERFORMANCE_CPU_PROFILE')) {
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
    $env:VPLAYER_PERFORMANCE_STATISTICS=$(if($Statistics -or $StatisticsReloads){'1'}else{'0'})
    $env:VPLAYER_PERFORMANCE_STATISTICS_RELOADS=$(if($StatisticsReloads){'1'}else{'0'})
    $env:VPLAYER_PERFORMANCE_PREPARE_PLAYLIST_QUERY=$(if($PreparePlaylistQuery){'1'}else{'0'})
    $env:VPLAYER_PERFORMANCE_MUSIC_PLAYLIST=$(if($MusicPlaylist){'1'}else{'0'})
    $env:VPLAYER_PERFORMANCE_MUSIC_PLAYLIST_SAVE=$(if($MusicPlaylistSave){'1'}else{'0'})
    $env:VPLAYER_PERFORMANCE_MUSIC_PLAYLIST_CLEAR=$(if($MusicPlaylistClear){'1'}else{'0'})
    $env:VPLAYER_PERFORMANCE_CPU_PROFILE=$(if($CpuProfile){'1'}else{'0'})
    $launch=@{
      FilePath=$app;WorkingDirectory=(Split-Path -Parent $app);PassThru=$true
      WindowStyle=$(if($Visible){'Normal'}else{'Hidden'})
      RedirectStandardOutput=(Join-Path $destination ('startup-'+$i+'.stdout.log'))
      RedirectStandardError=(Join-Path $destination ('startup-'+$i+'.stderr.log'))
    }
    $process=Start-Process @launch
    $traceProcess=$null
    $tracePath=Join-Path $destination ('startup-'+$i+'.nettrace')
    if($traceExecutable) {
      $traceLaunch=@{
        FilePath=$traceExecutable;WindowStyle='Hidden';PassThru=$true
        ArgumentList=@(
          'collect','--process-id',$process.Id,'--output',('"'+$tracePath+'"'),
          '--profile','dotnet-sampled-thread-time',
          '--providers','Microsoft-Windows-DotNETRuntime:0x40014001:4',
          '--duration','00:00:01:00'
        )
        RedirectStandardOutput=(Join-Path $destination ('startup-'+$i+'.trace.stdout.log'))
        RedirectStandardError=(Join-Path $destination ('startup-'+$i+'.trace.stderr.log'))
      }
      $traceProcess=Start-Process @traceLaunch
    }
    $deadline=[Diagnostics.Stopwatch]::StartNew()
    $exited=$false
    while($deadline.Elapsed.TotalSeconds -lt $timeoutSeconds) {
      $remaining=[Math]::Max(1,[Math]::Min(60000,$timeoutSeconds*1000-$deadline.ElapsedMilliseconds))
      if($process.WaitForExit([int]$remaining)){$exited=$true;break}
    }
    if(!$exited) {
      Stop-Process -Id $process.Id -Force
      if(!$process.WaitForExit(10000)){throw 'Benchmark process did not exit after termination.'}
      $record=@{Status='Timeout';Commit=$Commit;Phases=@();TimeoutSeconds=$timeoutSeconds}
      if(Test-Path -LiteralPath $result) {
        $record=Get-Content -LiteralPath $result -Raw | ConvertFrom-Json
        $record.Status='Timeout'
        $record | Add-Member -NotePropertyName TimeoutSeconds -NotePropertyValue $timeoutSeconds -Force
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
    if($traceProcess) {
      if(!$traceProcess.WaitForExit(30000)) {
        Stop-Process -Id $traceProcess.Id -Force
        throw 'Trace collector did not finish after the benchmark process exited.'
      }
      if($traceProcess.ExitCode -ne 0 -or !(Test-Path -LiteralPath $tracePath) -or (Get-Item -LiteralPath $tracePath).Length -eq 0) {
        throw 'Trace collector failed; retained collector logs and benchmark output.'
      }
      $profile=if($record.DiagnosticProfile){$record.DiagnosticProfile}else{'none'}
      if($record -is [Collections.IDictionary]) {
        $record['DiagnosticProfile']=$profile+'-eventpipe'
        $record['ExternalTrace']='dotnet-sampled-thread-time; CLR GC/contention/threading/stacks'
      } else {
        $record | Add-Member -NotePropertyName DiagnosticProfile -NotePropertyValue ($profile+'-eventpipe') -Force
        $record | Add-Member -NotePropertyName ExternalTrace -NotePropertyValue 'dotnet-sampled-thread-time; CLR GC/contention/threading/stacks' -Force
      }
    }
    $record | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $result
    Write-Output ('Startup '+$i+': '+$record.Status)
  }
} finally {
  foreach($name in $originalValues.Keys) {[Environment]::SetEnvironmentVariable($name,$originalValues[$name],'Process')}
}
