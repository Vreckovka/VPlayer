param(
 [string]$NativeBaseline=(Join-Path $PSScriptRoot 'iterations/vlc-startup-baseline-6d1fb461.json'),
 [string]$NativeOptimized=(Join-Path $PSScriptRoot 'iterations/vlc-startup-prepared-17f25291.json'),
 [string]$AppBaseline=(Join-Path $PSScriptRoot 'iterations/vlc-app-startup-baseline-3a69c4e8.json'),
 [string]$AppOptimized=(Join-Path $PSScriptRoot 'iterations/vlc-app-startup-prepared-17f25291.json'),
 [string]$Output=(Join-Path $PSScriptRoot 'vlc-startup-results.md')
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
function Native($path,$cached){
 $series=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
 if($series.Schema -ne 'vlc-startup-series-v1' -or $series.BenchmarkCommit -notmatch '^[0-9a-f]{40}$' -or $series.ProductionCommit -notmatch '^[0-9a-f]{40}$' -or $series.Samples.Count -ne 5){throw 'Invalid native series'}
 foreach($run in $series.Samples){
  if($run.Schema -ne 'vlc-startup-v1' -or $run.Commit -ne $series.ProductionCommit -or $run.CachePresent -ne $cached -or -not $run.FreshProcess -or $run.ColdDiskCache -or $run.DecodedFrames -ne 96000 -or $run.DurationMilliseconds -ne 2000 -or $run.DecodeFailures -ne 0 -or $run.TotalInitializationMilliseconds -le 0){throw 'Invalid native sample'}
  if($cached -and ($run.CacheBytes -le 0 -or $run.CacheSha256 -notmatch '^[0-9A-F]{64}$')){throw 'Invalid prepared cache'}
 }
 return $series
}
function NativeSame($a,$b){
 foreach($field in @('NativePayloadSha256','NativeVersion','NativeDlls','Runtime','Architecture','Configuration','DecodedFrames','DurationMilliseconds','DecodeFailures','NativeDirectory')){if($a.$field -ne $b.$field){throw ('Changed native workload '+$field)}}
 foreach($field in @('AudioFilters','VideoFilters')){if(($a.$field -join ',') -ne ($b.$field -join ',')){throw ('Changed native modules '+$field)}}
}
function Apps($path){
 $runs=@(Get-Content -LiteralPath $path -Raw|ConvertFrom-Json)
 if($runs.Count -ne 3){throw 'Invalid app series size'}
 foreach($run in $runs){
  if($run.Commit -notmatch '^[0-9a-f]{40}$' -or $run.Commit -ne $runs[0].Commit -or $run.Status -notin @('Rendered','Timeout') -or $run.FailureType -or $run.WindowMode -ne 'Visible' -or $run.MusicPlaylistEntries -ne 100000 -or $run.DiagnosticMode -ne 'buffered-v1' -or $run.DiagnosticProfile -ne 'none'){throw 'Invalid app workload'}
  if($run.Status -eq 'Timeout' -and $run.TimeoutSeconds -ne 60){throw 'Changed app timeout'}
  foreach($phase in @('Application / initial playlist view ready','VLC / core initialization')){if(@($run.Phases|Where-Object Name -eq $phase).Count -ne 1){throw ('Missing startup endpoint '+$phase)}}
  if(@($run.Phases|Where-Object Name -eq 'VLC / instance construction').Count -ne 2){throw 'Changed VLC instance count'}
  if($run.Status -eq 'Rendered'){
   foreach($endpoint in @('Initial music view','100k music playlist','Last music track','long no-match music search','long near-match music search')){if($run.Observations.('UI / music playlist / painted '+$endpoint) -ne 1){throw ('Missing painted endpoint '+$endpoint)}}
   if($run.Observations.'UI / music playlist / ordered occurrence check' -ne 1 -or $run.Observations.'UI / music playlist / items' -ne 100000 -or $run.Observations.'UI / music playlist / last track visible' -ne 1 -or $run.Observations.'UI / music playlist / stored metadata ready playlist' -le 0){throw 'Missing music correctness check'}
  }
 }
 return $runs
}
function Phase($run,$name){return [double](@($run.Phases|Where-Object Name -eq $name)[0].Milliseconds)}
function Construction($run){return [double](($run.Phases|Where-Object Name -eq 'VLC / instance construction'|Measure-Object Milliseconds -Sum).Sum)}
function Peak($values){return [double](($values|Measure-Object -Maximum).Maximum)}
function Change($a,$b){
 $delta=100*($b-$a)/$a
 if($delta -gt -100 -and $delta -lt 100 -and [math]::Abs([math]::Round($delta,1)) -eq 100){return ' ('+$delta.ToString('0.00',[Globalization.CultureInfo]::InvariantCulture)+'%)'}
 return Format-Change $a $b
}
$before=Native $NativeBaseline $false
$after=Native $NativeOptimized $true
if($before.ProductionCommit -ne $after.ProductionCommit -or $before.BenchmarkCommit -ne $after.BenchmarkCommit){throw 'Changed native provider or benchmark'}
foreach($run in @($before.Samples)+@($after.Samples)){NativeSame $before.Samples[0] $run}
$appBefore=@(Apps $AppBaseline)
$appAfter=@(Apps $AppOptimized)
foreach($run in @($appBefore)+@($appAfter)){
 foreach($field in @('FixtureSha256','Runtime','Configuration','ProcessorCount','OS','WindowMode','MusicPlaylistEntries','SoundItems','Playlists','PlaylistQueryPreparation')){if($run.$field -ne $appBefore[0].$field){throw ('Changed app workload '+$field)}}
}
$painted=@(@($appBefore)+@($appAfter)|Where-Object Status -eq 'Rendered')
foreach($run in $painted){foreach($query in @('long no-match','long near-match')){foreach($suffix in @('matches','ordered ids hash','query length')){$key='UI / music playlist / '+$query+' '+$suffix;if($run.Observations.$key -ne $painted[0].Observations.$key){throw ('Changed rendered search '+$key)}}}}
$lines=[Collections.Generic.List[string]]::new()
$lines.Add('# Startup: prepared VLC plugins')
$lines.Add('')
$lines.Add('Fresh processes, same native payload. Visible published app with the copied 207k-item library and 100k playlist. Initial first-launch delays are retained; no disk-cache purge or restart was performed. These are not cold-after-restart measurements.')
$lines.Add('')
$lines.Add('| Baseline now | New optimized version |')
$lines.Add('| --- | --- |')
function Row($name,$a,$b){$lines.Add('| **'+$name+'** — '+(Format-Time $a)+' | '+(Format-Time $b)+(Change $a $b)+' |')}
Row 'App / first initial library view' (Phase $appBefore[0] 'Application / initial playlist view ready') (Phase $appAfter[0] 'Application / initial playlist view ready')
Row 'App / slowest initial library view' (Peak @($appBefore|ForEach-Object {Phase $_ 'Application / initial playlist view ready'})) (Peak @($appAfter|ForEach-Object {Phase $_ 'Application / initial playlist view ready'}))
Row 'App / slowest VLC construction total' (Peak @($appBefore|ForEach-Object {Construction $_})) (Peak @($appAfter|ForEach-Object {Construction $_}))
Row 'Component / first native initialization' $before.Samples[0].TotalInitializationMilliseconds $after.Samples[0].TotalInitializationMilliseconds
Row 'Component / slowest native initialization' (Peak $before.Samples.TotalInitializationMilliseconds) (Peak $after.Samples.TotalInitializationMilliseconds)
Row 'Component / slowest native library loading' (Peak $before.Samples.NativeLoadMilliseconds) (Peak $after.Samples.NativeLoadMilliseconds)
$lines.Add('')
$lines.Add('The publish profile now generates a fresh `plugins.dat` for its native VLC version. Native module lists and complete PCM decoding matched. Relocation, regeneration and invalid/unwritable payload checks passed. [VideoLAN guidance](https://docs.videolan.me/libvlcsharp/docs/best_practices.html).')
$lines.Add('')
$missingBefore=@($appBefore|Where-Object Status -eq 'Timeout').Count
$missingAfter=@($appAfter|Where-Object Status -eq 'Timeout').Count
$lines.Add("Incomplete 100k playlist checks: baseline $missingBefore; optimized $missingAfter. The table uses completed initial-library-view boundaries even when a later playlist check timed out; incomplete runs do not establish playlist-load speed or correctness.")
$lines.Add('')
$lines.Add('Raw samples: [native baseline](iterations/vlc-startup-baseline-6d1fb461.json), [native optimized](iterations/vlc-startup-prepared-17f25291.json), [app baseline](iterations/vlc-app-startup-baseline-3a69c4e8.json), [app optimized](iterations/vlc-app-startup-prepared-17f25291.json), [cache checks](iterations/vlc-cache-checks-17f25291.json), [publish and visual validation](iterations/vlc-startup-validation-17f25291.json). The initial native validation sample belongs to production source `6d1fb461`; its benchmark was subsequently frozen as `4f533a3c`.')
$lines.Add('')
$lines.Add('Only the agent-owned published build was changed. The running user app and installed files were left untouched. This optimization has not been deployed.')
[IO.File]::WriteAllLines($Output,$lines,[Text.UTF8Encoding]::new($true))