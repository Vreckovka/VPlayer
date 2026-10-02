param(
  [Parameter(Mandatory=$true)][string]$Baseline,
  [string]$Optimized,
  [string]$Output=(Join-Path $PSScriptRoot 'grouped-ui-results.md')
)
$ErrorActionPreference='Stop'
function Read-Runs([string]$source) {
  if(Test-Path -LiteralPath $source -PathType Container) {
    $files=@(Get-ChildItem -LiteralPath $source -Filter 'startup-*.json' | Sort-Object Name)
    if($files.Count -eq 0) {throw 'No startup samples.'}
    return @($files | ForEach-Object {Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json})
  }
  return @(Get-Content -LiteralPath $source -Raw | ConvertFrom-Json)
}
function Median($values) {
  $sorted=@($values | Sort-Object)
  if($sorted.Count -eq 0) {return $null}
  if($sorted.Count % 2) {return $sorted[[int][Math]::Floor($sorted.Count/2)]}
  return ($sorted[$sorted.Count/2-1]+$sorted[$sorted.Count/2])/2
}
function Timing($runs,[string]$name) {
  if(!$runs) {return 'Pending'}
  $values=@($runs | ForEach-Object {$_.Phases | Where-Object Name -eq $name | ForEach-Object Milliseconds})
  if(!$values.Count) {return 'Not reached (0/'+$runs.Count+')'}
  return ('{0:N2} ms median; {1:N2} ms max; {2}/{3} reached' -f (Median $values),($values | Measure-Object -Maximum).Maximum,$values.Count,$runs.Count)
}
function Observation($runs,[string]$name) {
  if(!$runs) {return 'Pending'}
  $values=@($runs | Where-Object {$_.Observations.PSObject.Properties.Name -contains $name} | ForEach-Object {$_.Observations.$name})
  if(!$values.Count) {return 'Not reached'}
  return ('{0:N0} median; {1:N0} max' -f (Median $values),($values | Measure-Object -Maximum).Maximum)
}
function Outcome($runs) {
  if(!$runs) {return 'Pending'}
  $successful=@($runs | Where-Object {$_.Status -eq 'Rendered' -and $_.Observations.'UI / last favorite visible' -eq 1}).Count
  $timeouts=@($runs | Where-Object Status -eq 'Timeout').Count
  $failures=$runs.Count-$successful-$timeouts
  return "$successful/$($runs.Count) completed; $timeouts timed out at 60 s; $failures failed"
}
$a=Read-Runs $Baseline
$b=if($Optimized){Read-Runs $Optimized}else{@()}
foreach($runs in @(@{Samples=$a},@{Samples=$b})) {
  if(!$runs.Samples.Count){continue}
  foreach($field in @('Commit','FixtureSha256','SoundItems','Playlists','WindowMode','Runtime','Configuration','ProcessorCount','OS')) {
    $values=@($runs.Samples | ForEach-Object {$_.$field} | Select-Object -Unique)
    if($values.Count -ne 1 -or $null -eq $values[0]) {throw "Missing or inconsistent $field."}
  }
}
if($b.Count) {
  foreach($field in @('FixtureSha256','SoundItems','Playlists','WindowMode','Runtime','Configuration','ProcessorCount','OS')) {
    if($a[0].$field -ne $b[0].$field) {throw "Incompatible grouped UI runs: $field."}
  }
}
$lines=@(
  '# Grouped playlist UI — worst-case comparison'
  ''
  ('Same disposable expanded library: {0:N0} sound items, {1:N0} playlists, including 5,000 additional favorites with long titles. Release x64, visible WPF windows, fresh profile per launch.' -f $a[0].SoundItems,$a[0].Playlists)
  ''
  '| Baseline now | New optimized version |'
  '| --- | --- |'
  ('| Complete render and last-favorite scroll: '+(Outcome $a)+' | '+(Outcome $b)+' |')
  ('| Populated playlist view: '+(Timing $a 'Application / initial playlist view ready')+' | '+(Timing $b 'Application / initial playlist view ready')+' |')
  ('| First window render: '+(Timing $a 'Application / first window render')+' | '+(Timing $b 'Application / first window render')+' |')
  ('| Scroll to last favorite: '+(Timing $a 'UI / grouped playlists / scroll to last favorite')+' | '+(Timing $b 'UI / grouped playlists / scroll to last favorite')+' |')
  ('| Realized playlist rows at ready: '+(Observation $a 'UI / realized playlist rows')+' | '+(Observation $b 'UI / realized playlist rows')+' |')
  ('| Realized playlist rows after scroll: '+(Observation $a 'UI / realized playlist rows after scroll')+' | '+(Observation $b 'UI / realized playlist rows after scroll')+' |')
  ('| Commit: '+$a[0].Commit+' | '+$(if($b.Count){$b[0].Commit}else{'Pending'})+' |')
  ''
  ('Fixture SHA-256: '+$a[0].FixtureSha256+'.')
  ''
  'The ready milestone requires a real nonempty rendered row. The scroll measurement requires the last favorite row to fit inside its scroll viewport. Screenshot files verify the actual application template locally. Counts include all realized row containers in the grouped view.'
  ''
  'A timeout is a failed workload, not a timing improvement. First-frame durations from launches that later time out remain partial observations; they do not establish a successful startup. Baseline and optimized samples must match fixture, runtime, configuration, machine and window mode. Durations include process startup; the scroll scope excludes screenshot capture.'
  ''
  'This scenario measures the grouped music-playlist view. Other app features retain their separate baselines and pending coverage in results.md.'
)
$lines | Set-Content -LiteralPath $Output
Write-Output $Output
