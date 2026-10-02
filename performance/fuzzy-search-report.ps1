param(
  [string]$Baseline=(Join-Path $PSScriptRoot 'fuzzy-search-baseline.json'),
  [string]$Optimized,
  [string]$Output=(Join-Path $PSScriptRoot 'fuzzy-search-results.md')
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
$fields=@('SchemaVersion','FixtureSha256','Runtime','Configuration','ProcessorCount','OS')
function Same($a,$b,$names) {
  foreach($name in $names) {if($null -eq $a.$name -or $null -eq $b.$name -or $a.$name -ne $b.$name){throw ('Missing or changed search field: '+$name)}}
}
function Read-Series($path) {
  if(!$path){return @()}
  $runs=@(Get-Content -LiteralPath $path -Raw|ConvertFrom-Json)
  if(!$runs.Count){throw 'Empty search series'}
  foreach($run in $runs) {
    if($run.Commit -notmatch '^[0-9a-fA-F]{40}$' -or $run.Commit -ne $runs[0].Commit){throw 'Missing or mixed search commit'}
    Same $runs[0] $run $fields
    if($run.Metrics.Count -ne 2 -or $run.Results.Count -ne 2){throw 'Changed search scenario count'}
    foreach($name in @('UI / library fuzzy long no-match','UI / library fuzzy long near-match')) {
      $metric=@($run.Metrics|Where-Object Name -eq $name)
      $result=@($run.Results|Where-Object Name -eq $name)
      if($metric.Count -ne 1 -or $result.Count -ne 1 -or $metric[0].SamplesMilliseconds.Count -lt 2 -or
         @($metric[0].SamplesMilliseconds|Where-Object {$_ -le 0}).Count -or $result[0].OrderedIdsSha256 -notmatch '^[0-9A-F]{64}$'){throw 'Invalid search metric or result'}
      Same ($runs[0].Metrics|Where-Object Name -eq $name) $metric[0] @('Name','Category','Boundary','Workload')
      Same ($runs[0].Results|Where-Object Name -eq $name) $result[0] @('Name','Query','Count','OrderedIdsSha256')
    }
  }
  return $runs
}
function Timing($runs,$name,$first) {
  $values=foreach($run in $runs) {
    $metric=$run.Metrics|Where-Object Name -eq $name
    if($first){$metric.SamplesMilliseconds[0]}else{$metric.SamplesMilliseconds|Select-Object -Skip 1}
  }
  $sorted=@($values|Sort-Object)
  $middle=[int][Math]::Floor($sorted.Count/2)
  if($sorted.Count%2){return $sorted[$middle]}
  return ($sorted[$middle-1]+$sorted[$middle])/2
}
$a=Read-Series $Baseline
$b=Read-Series $Optimized
if($b.Count) {
  Same $a[0] $b[0] $fields
  foreach($metric in $a[0].Metrics) {
    Same $metric ($b[0].Metrics|Where-Object Name -eq $metric.Name) @('Name','Category','Boundary','Workload')
    Same ($a[0].Results|Where-Object Name -eq $metric.Name) ($b[0].Results|Where-Object Name -eq $metric.Name) @('Name','Query','Count','OrderedIdsSha256')
  }
}
$rows=foreach($metric in $a[0].Metrics) {
  foreach($first in @($true,$false)) {
    [pscustomobject]@{Name=($metric.Name -replace '^UI / library fuzzy ','')+' / '+$(if($first){'first search'}else{'repeat search'});
      Old=(Timing $a $metric.Name $first);New=$(if($b.Count){Timing $b $metric.Name $first}else{$null})}
  }
}
$lines=@('# Library fuzzy search','','Full copied library; longest-title queries. Production filtering and result publication; no loading or XAML. - % = less time; + % = more time.','','| Baseline now | New optimized version |','| --- | --- |')
foreach($row in @($rows|Sort-Object Old -Descending)) {
  $new=if($null -ne $row.New){(Format-Time $row.New)+(Format-Change $row.Old $row.New)}else{'Pending'}
  $lines+='| **'+$row.Name+'** — '+(Format-Time $row.Old)+' | '+$new+' |'
}
$lines+=@('','First samples stay separate from repeats. Raw timings, allocations, matching result hashes and provenance stay in JSON.')
$lines|Set-Content -LiteralPath $Output
Write-Output ('Wrote '+$Output)
