param(
  [Parameter(Mandatory=$true)][string]$Baseline,
  [Parameter(Mandatory=$true)][string]$Optimized,
  [Parameter(Mandatory=$true)][string]$RepeatedBaseline,
  [Parameter(Mandatory=$true)][string]$RepeatedOptimized,
  [string]$Output=(Join-Path $PSScriptRoot 'playlist-collection-results.md')
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
function Read-Collection($path) {
  $run=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
  if($run.SchemaVersion -ne 1 -or $run.Commit -notmatch '^[0-9a-f]{40}$' -or $run.Entries -ne 100000){throw 'Invalid collection protocol/provenance'}
  if(@($run.Samples).Count -ne 5){throw 'Requires five complete collection samples'}
  for($i=0;$i -lt 5;$i++) {
    $sample=$run.Samples[$i]
    if($sample.Iteration -ne $i -or $sample.AddMilliseconds -le 0 -or $sample.ClearMilliseconds -lt 0 -or
      $sample.AddAllocatedBytes -le 0 -or $null -eq $sample.ClearAllocatedBytes){throw 'Incomplete collection sample'}
  }
  return $run
}
function Same-Collection($first,$second) {
  foreach($field in @('SchemaVersion','Entries','DuplicateTracks','MissingFileInfo','FixtureSha256','Runtime','Configuration','ProcessorCount','Boundary')) {
    if($null -eq $first.$field -or $null -eq $second.$field -or $first.$field -ne $second.$field){throw ('Changed collection workload: '+$field+' ['+$first.$field+' / '+$second.$field+']; '+$first.GetType().Name+' / '+$second.GetType().Name)}
  }
  $a=if($first.SourceOccurrences){$first.SourceOccurrences}else{'original 100k stored occurrences'}
  $b=if($second.SourceOccurrences){$second.SourceOccurrences}else{'original 100k stored occurrences'}
  if($a -ne $b){throw 'Changed source occurrences'}
}
function Median-Collection($run,$field) {
  $values=@($run.Samples | Select-Object -Skip 1 | ForEach-Object {$_.$field} | Sort-Object)
  return ($values[1]+$values[2])/2
}
function Memory-Collection($bytes) {
  if($bytes -ge 1GB){return ($bytes/1GB).ToString('0.#',[Globalization.CultureInfo]::InvariantCulture)+' GiB'}
  if($bytes -ge 1MB){return ($bytes/1MB).ToString('0.#',[Globalization.CultureInfo]::InvariantCulture)+' MiB'}
  if($bytes -ge 1KB){return ($bytes/1KB).ToString('0.#',[Globalization.CultureInfo]::InvariantCulture)+' KiB'}
  return ([double]$bytes).ToString('0',[Globalization.CultureInfo]::InvariantCulture)+' B'
}
$unique=Read-Collection $Baseline
$uniqueOptimized=Read-Collection $Optimized
$repeated=Read-Collection $RepeatedBaseline
$repeatAfter=Read-Collection $RepeatedOptimized
Same-Collection $unique $uniqueOptimized
Same-Collection $repeated $repeatAfter
if($unique.DuplicateTracks -ne 0 -or $repeated.DuplicateTracks -ne 50000){throw 'Missing unique or repeated-track worst case'}
$lines=[Collections.Generic.List[string]]::new()
$lines.Add('# Playlist collection')
$lines.Add('')
foreach($pair in @(@{Title='100k unique track IDs';Before=$unique;After=$uniqueOptimized},@{Title='100k occurrences / 50k track IDs';Before=$repeated;After=$repeatAfter})) {
  $lines.Add('## '+$pair.Title)
  $lines.Add('')
  $lines.Add('| Baseline | Optimized |')
  $lines.Add('| --- | --- |')
  foreach($row in @(@{Label='Publish first';Field='AddMilliseconds';First=$true},@{Label='Publish repeat';Field='AddMilliseconds'},@{Label='Publish allocation';Field='AddAllocatedBytes';Memory=$true},@{Label='Clear repeat';Field='ClearMilliseconds'})) {
    $a=if($row.First){$pair.Before.Samples[0].($row.Field)}else{Median-Collection $pair.Before $row.Field}
    $b=if($row.First){$pair.After.Samples[0].($row.Field)}else{Median-Collection $pair.After $row.Field}
    $before=if($row.Memory){Memory-Collection $a}else{Format-Time $a}
    $after=if($row.Memory){Memory-Collection $b}else{Format-Time $b}
    $lines.Add('| '+$row.Label+' — '+$before+' | '+$after+(Format-Change $a $b)+' |')
  }
  $lines.Add('')
}
$lines.Add('Collection publication with real saved song views, synchronous membership observers and WPF ListCollectionView. Database reads, view construction, dispatcher scheduling, disposal and painting are excluded. Repeat values use the post-first samples; raw JSON retains every sample.')
$lines.Add('')
$lines.Add('Clear includes subscription cleanup, which the baseline omitted. Removed/cleared rows no longer report updates; replacement views stay current, duplicate references retain one update per remaining occurrence, and moves preserve membership.')
$lines.Add('')
$lines.Add('Native application results are reported separately. These component timings do not establish cold-start or full UI gains.')
$lines.Add('')
$lines.Add('Source: '+$unique.Commit.Substring(0,8)+' / '+$repeated.Commit.Substring(0,8)+' → '+$uniqueOptimized.Commit.Substring(0,8)+'.')
$lines | Set-Content -LiteralPath $Output -Encoding UTF8
Write-Output ('Wrote '+$Output)