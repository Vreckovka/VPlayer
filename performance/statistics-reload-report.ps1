param(
  [string]$BaselineData=(Join-Path $PSScriptRoot 'statistics-reload-baseline.json'),
  [string]$OptimizedData,
  [string]$BaselineUI=(Join-Path $PSScriptRoot 'statistics-reload-ui-baseline.json'),
  [string]$OptimizedUI,
  [string]$Output=(Join-Path $PSScriptRoot 'statistics-reload-results.md')
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
function Assert-Fields($a,$b,$fields) {
  foreach($field in $fields) {
    if($null -eq $a.$field -or $null -eq $b.$field -or $a.$field -ne $b.$field){throw ('Missing or incompatible reload field: '+$field)}
  }
}
function Read-Series($path,$ui) {
  if(!$path){return @()}
  $runs=@(Get-Content -LiteralPath $path -Raw | ConvertFrom-Json)
  if(!$runs.Count){throw 'Empty reload series'}
  $fields=if($ui){@('FixtureSha256','Runtime','Configuration','ProcessorCount','OS','WindowMode','SoundItems','Playlists')}else{@('SchemaVersion','FixtureSha256','Runtime','Configuration','ProcessorCount','OS','ReloadRequests')}
  foreach($run in $runs) {
    if($run.Commit -notmatch '^[0-9a-fA-F]{40}$' -or $run.Commit -ne $runs[0].Commit){throw 'Missing or mixed reload commit'}
    Assert-Fields $runs[0] $run $fields
    if(!$ui){Assert-Fields $runs[0].Metrics[0] $run.Metrics[0] @('Name','Category','Boundary','Workload')}
  }
  if($ui){Assert-DiagnosticMode $runs @()}
  return $runs
}
function Median($values) {
  $sorted=@($values|Sort-Object)
  if(!$sorted.Count){throw 'No completed reload measurements'}
  $middle=[int][Math]::Floor($sorted.Count/2)
  if($sorted.Count%2){return $sorted[$middle]}
  return ($sorted[$middle-1]+$sorted[$middle])/2
}
function Data-Timing($runs,$first) {
  $values=foreach($run in $runs) {
    $metric=@($run.Metrics|Where-Object Name -eq 'UI / statistics reload burst')
    if($metric.Count -ne 1 -or $run.ReloadRequests -ne 32 -or $metric[0].SamplesMilliseconds.Count -lt 2){throw 'Invalid reload workload or metric'}
    if($first){$metric[0].SamplesMilliseconds[0]}else{$metric[0].SamplesMilliseconds|Select-Object -Skip 1}
  }
  return Median $values
}
function UI-Timing($runs) {
  $complete=@($runs|Where-Object {
    $_.Status -eq 'Rendered' -and @($_.Phases|Where-Object Name -eq 'UI / statistics / reload burst and render').Count -eq 1 -and
    $_.Observations.'UI / statistics / reload requests' -eq 32 -and
    $_.Observations.'UI / statistics / reload empty item rows' -eq 0 -and
    $_.Observations.'UI / statistics / reload empty playlist rows' -eq 0
  })
  $values=@($complete|ForEach-Object {($_.Phases|Where-Object Name -eq 'UI / statistics / reload burst and render').Milliseconds})
  return [pscustomobject]@{Value=(Median $values);Partial=($complete.Count -ne $runs.Count)}
}
$a=Read-Series $BaselineData $false
$b=Read-Series $OptimizedData $false
$c=Read-Series $BaselineUI $true
$d=Read-Series $OptimizedUI $true
if($b.Count) {
  Assert-Fields $a[0] $b[0] @('SchemaVersion','FixtureSha256','Runtime','Configuration','ProcessorCount','OS','ReloadRequests')
  Assert-Fields $a[0].Metrics[0] $b[0].Metrics[0] @('Name','Category','Boundary','Workload')
}
if($d.Count) {
  Assert-DiagnosticMode $c $d
  Assert-Fields $c[0] $d[0] @('FixtureSha256','Runtime','Configuration','ProcessorCount','OS','WindowMode','SoundItems','Playlists')
}
if($a[0].FixtureSha256 -ne $c[0].FixtureSha256){throw 'Reload data/UI fixtures differ'}
$rows=@(
  [pscustomobject]@{Name='first burst / data and UI publication';Old=[pscustomobject]@{Value=(Data-Timing $a $true);Partial=$false};New=$(if($b.Count){[pscustomobject]@{Value=(Data-Timing $b $true);Partial=$false}}else{$null})},
  [pscustomobject]@{Name='repeat burst / data and UI publication';Old=[pscustomobject]@{Value=(Data-Timing $a $false);Partial=$false};New=$(if($b.Count){[pscustomobject]@{Value=(Data-Timing $b $false);Partial=$false}}else{$null})},
  [pscustomobject]@{Name='reload burst / load and render';Old=(UI-Timing $c);New=$(if($d.Count){UI-Timing $d}else{$null})}
)
$lines=[Collections.Generic.List[string]]::new()
$lines.Add('# Statistics reload bursts')
$lines.Add('')
$lines.Add('32 rapid Load requests; 207k fully played items and 5,310 playlists. Median times; - % = less time, + % = more time.')
$lines.Add('')
$lines.Add('| Baseline now | New optimized version |')
$lines.Add('| --- | --- |')
foreach($row in @($rows|Sort-Object {$_.Old.Value} -Descending)) {
  $old=(Format-Time $row.Old.Value)+$(if($row.Old.Partial){'*'}else{''})
  $new=if($row.New){(Format-Time $row.New.Value)+(Format-Change $row.Old.Value $row.New.Value)+$(if($row.New.Partial){'*'}else{''})}else{'Pending'}
  $lines.Add('| **'+$row.Name+'** — '+$old+' | '+$new+' |')
}
$lines.Add('')
$lines.Add('Data rows include production loading and dispatcher publication. The WPF row includes rendering in the actual app. Full samples, failures, repository counts and provenance stay in JSON.')
if(@($rows|Where-Object {$_.Old.Partial -or $_.New.Partial}).Count){$lines.Add('* Completed timings; incomplete launches stay in JSON.')}
$lines | Set-Content -LiteralPath $Output
