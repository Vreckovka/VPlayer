param(
  [string]$BaselineData=(Join-Path $PSScriptRoot 'statistics-baseline.json'),
  [string]$OptimizedData,
  [string]$BaselineUI=(Join-Path $PSScriptRoot 'statistics-ui-baseline.json'),
  [string]$OptimizedUI,
  [string]$Output=(Join-Path $PSScriptRoot 'statistics-results.md')
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
function Read-Json($path) {
  if($path) {return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json}
  return $null
}
function Assert-Fields($a,$b,$fields) {
  foreach($field in $fields) {
    if($null -eq $a.$field) {throw "Missing baseline field: $field"}
    if($b -and ($null -eq $b.$field -or $a.$field -ne $b.$field)) {throw "Incompatible Statistics runs: $field"}
  }
}
function Median($values) {
  $sorted=@($values|Sort-Object)
  if(!$sorted.Count){return $null}
  if($sorted.Count%2){return $sorted[[int][Math]::Floor($sorted.Count/2)]}
  return ($sorted[$sorted.Count/2-1]+$sorted[$sorted.Count/2])/2
}
function Complete-UI($run) {
  return $run.Status -eq 'Rendered' -and
    @($run.Phases|Where-Object Name -eq 'UI / statistics / load and render').Count -eq 1 -and
    $run.Observations.PSObject.Properties.Name -contains 'UI / statistics / empty item rows' -and
    $run.Observations.PSObject.Properties.Name -contains 'UI / statistics / empty playlist rows'
}
function UI-Time($runs) {
  if(!$runs.Count){return $null}
  foreach($run in $runs) {
    Assert-Fields $runs[0] $run @('Commit','FixtureSha256','SoundItems','Playlists','WindowMode','Runtime','Configuration','ProcessorCount','OS')
  }
  $complete=@($runs|Where-Object{Complete-UI $_})
  if(!$complete.Count){throw 'Statistics UI runs did not complete.'}
  return Median @($complete|ForEach-Object{($_.Phases|Where-Object Name -eq 'UI / statistics / load and render').Milliseconds})
}
$a=Read-Json $BaselineData
$b=Read-Json $OptimizedData
Assert-Fields $a $b @('SchemaVersion','FixtureSha256','Runtime','Configuration','ProcessorCount','OS')
$metric=@($a.Metrics|Where-Object Name -eq 'Data / statistics')
if($metric.Count -ne 1){throw 'Missing or duplicate Statistics baseline metric'}
$new=@($b.Metrics|Where-Object Name -eq 'Data / statistics')
if($b -and $new.Count -ne 1){throw 'Missing or duplicate optimized Statistics metric'}
if($new.Count) {Assert-Fields $metric[0] $new[0] @('Name','Category','Boundary','Workload')}
$oldFirst=$metric[0].FirstMilliseconds
$newFirst=if($new.Count){$new[0].FirstMilliseconds}else{$null}
$oldData=$metric[0].MedianMilliseconds
$newData=if($new.Count){$new[0].MedianMilliseconds}else{$null}
$oldUIRuns=@(Read-Json $BaselineUI)
$newUIRuns=if($OptimizedUI){@(Read-Json $OptimizedUI)}else{@()}
$oldUI=UI-Time $oldUIRuns
$incomplete=@($newUIRuns|Where-Object{!(Complete-UI $_)}).Count -gt 0
$newUI=UI-Time $newUIRuns
if($newUIRuns.Count) {
  Assert-Fields $oldUIRuns[0] $newUIRuns[0] @('FixtureSha256','SoundItems','Playlists','WindowMode','Runtime','Configuration','ProcessorCount','OS')
}
if($a.FixtureSha256 -ne $oldUIRuns[0].FixtureSha256){throw 'Statistics data and UI fixtures differ'}
function New-Time($baseline,$value) {
  if($null -eq $value){return 'Pending'}
  return (Format-Time $value)+(Format-Change $baseline $value)
}
@(
  $(if($a.Fixture.AllSoundsPlayed){'# Statistics — fully played library'}else{'# Statistics'})
  ''
  $(if($a.Fixture.AllSoundsPlayed){'207k played sound items with unique metadata and play times; 5,310 playlists.'}else{'207k sound items with unique file metadata; 5,310 playlists.'})
  'First data load and repeat medians; - % = less time, + % = more time.'
  ''
  '| Baseline now | New optimized version |'
  '| --- | --- |'
  ('| **first data load** — '+(Format-Time $oldFirst)+' | '+(New-Time $oldFirst $newFirst)+' |')
  ('| **warm data load** — '+(Format-Time $oldData)+' | '+(New-Time $oldData $newData)+' |')
  ('| **load and render** — '+(Format-Time $oldUI)+' | '+(New-Time $oldUI $newUI)+$(if($incomplete){'*'}else{''})+' |')
  ''
  $(if($incomplete){'* UI timing uses completed Statistics samples; raw JSON retains the incomplete launch.'})
  ''
  'Data timing includes production loading and UI publication. View timing includes navigation and actual WPF rendering. Full samples, allocations, row checks and provenance remain in the Statistics JSON files.'
) | Set-Content -LiteralPath $Output
Write-Output ('Wrote '+$Output)
