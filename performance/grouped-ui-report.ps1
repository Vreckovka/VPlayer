param(
  [Parameter(Mandatory=$true)][string]$Baseline,
  [string]$Optimized,
  [string]$Output=(Join-Path $PSScriptRoot 'grouped-ui-results.md')
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
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
function Timing-Value($runs,[string]$name) {
  $values=@($runs | ForEach-Object {$_.Phases | Where-Object Name -eq $name | ForEach-Object Milliseconds})
  if(!$values.Count){return $null}
  return Median $values
}
function Timing($runs,[string]$name) {
  if(!$runs) {return 'Pending'}
  $value=Timing-Value $runs $name
  if($null -eq $value) {
    if($name -eq 'Application / initial playlist view ready' -and @($runs | Where-Object Status -eq 'Timeout').Count) {return 'Timeout (60k ms)'}
    return 'n/a'
  }
  return Format-Time $value
}
function Comparison($baseline,$runs,[string]$name) {
  return (Timing $runs $name)+(Format-Change (Timing-Value $baseline $name) (Timing-Value $runs $name))
}
function Observation($runs,[string]$name) {
  if(!$runs) {return 'Pending'}
  $values=@($runs | Where-Object {$_.Observations.PSObject.Properties.Name -contains $name} | ForEach-Object {$_.Observations.$name})
  if(!$values.Count) {return 'n/a'}
  return (Median $values).ToString('0',[Globalization.CultureInfo]::InvariantCulture)
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
  '# Grouped playlist UI'
  ''
  '207k sound items; 5,310 playlists including 5k added favorites. Median timings; - % = less time, + % = more time.'
  ''
  '| Baseline now | New optimized version |'
  '| --- | --- |'
  ('| Populated view: '+(Timing $a 'Application / initial playlist view ready')+' | '+(Comparison $a $b 'Application / initial playlist view ready')+' |')
  ('| First frame*: '+(Timing $a 'Application / first window render')+' | '+(Comparison $a $b 'Application / first window render')+' |')
  ('| Scroll to last favorite: '+(Timing $a 'UI / grouped playlists / scroll to last favorite')+' | '+(Comparison $a $b 'UI / grouped playlists / scroll to last favorite')+' |')
  ('| Realized rows (ready / scrolled): '+(Observation $a 'UI / realized playlist rows')+' / '+(Observation $a 'UI / realized playlist rows after scroll')+' | '+(Observation $b 'UI / realized playlist rows')+' / '+(Observation $b 'UI / realized playlist rows after scroll')+' |')
  ''
  '*The baseline rendered its first frame, then timed out before becoming usable. Timeout comparisons have no percentage. Full samples and provenance are retained in grouped-ui-baseline.json and grouped-ui-optimized.json.'
)
$lines | Set-Content -LiteralPath $Output
Write-Output $Output
