param(
  [string]$Baseline=(Join-Path $PSScriptRoot 'music-playlist-ui-baseline.json'),
  [string]$Optimized,
  [string]$EnrichmentBaseline=(Join-Path $PSScriptRoot 'iterations/music-enrichment-baseline-0fcece16.json'),
  [string]$Output=(Join-Path $PSScriptRoot 'music-playlist-ui-results.md')
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
$fields=@('FixtureSha256','Runtime','Configuration','ProcessorCount','OS','WindowMode','MusicPlaylistEntries','SoundItems','Playlists','PlaylistQueryPreparation')
function Same($a,$b) {
  foreach($name in $fields) {if($null -eq $a.$name -or $null -eq $b.$name -or $a.$name -ne $b.$name){throw ('Missing or changed music field: '+$name)}}
}
function Read-Series($path) {
  if(!$path){return @()}
  $runs=@(Get-Content -LiteralPath $path -Raw|ConvertFrom-Json)
  if(!$runs.Count){throw 'Empty music series'}
  foreach($run in $runs) {
    if($run.Commit -notmatch '^[0-9a-fA-F]{40}$' -or $run.Commit -ne $runs[0].Commit){throw 'Missing or mixed music commit'}
    Same $runs[0] $run
    if($run.MusicPlaylistEntries -ne 100000 -or $run.WindowMode -ne 'Visible' -or
       $run.DiagnosticMode -ne 'buffered-v1' -or $run.DiagnosticProfile -ne 'none'){throw 'Changed music workload or diagnostic protocol'}
    if($run.Status -notin @('Rendered','Timeout') -or $run.FailureType){throw 'Music run failed before a valid timing endpoint'}
    if($run.Status -eq 'Timeout' -and ($run.TimeoutSeconds -ne 60 -or
       'UI / music playlist / load and render' -notin $run.ActivePhases -or
       'UI / player playlist / collection publication' -notin $run.ActivePhases)){
      throw 'Timeout does not establish the expected unfinished large playlist load'
    }
    if(@($run.Phases|Where-Object Name -eq 'UI / music playlist / read and create incoming views').Count -ne 1){throw 'Missing or ambiguous large playlist read phase'}
    if($run.Status -eq 'Rendered' -and ($run.Observations.'UI / music playlist / ordered occurrence check' -ne 1 -or
       $run.Observations.'UI / music playlist / items' -ne 100000 -or $run.Observations.'UI / music playlist / last track visible' -ne 1)){
      throw 'Rendered music run lacks complete ordered playlist/viewport checks'
    }
    if($run.Status -eq 'Rendered') {
      foreach($endpoint in @('Initial music view','100k music playlist','Last music track','long no-match music search','long near-match music search')) {
        if($run.Observations.('UI / music playlist / painted '+$endpoint) -ne 1){throw ('Missing painted music endpoint: '+$endpoint)}
      }
      if(@($run.ActivePhases|Where-Object {$_ -like 'UI / music playlist /*' -or $_ -like 'UI / player playlist /*'}).Count){throw 'Music measurements are still active'}
      foreach($phase in @('load and render','scroll to last track','long no-match search and render','long near-match search and render')){
        $completed=@($run.Phases|Where-Object Name -eq ('UI / music playlist / '+$phase))
        if($completed.Count -ne 1 -or $completed[0].Milliseconds -le 0){throw ('Missing music endpoint: '+$phase)}
      }
      if($run.Observations.'UI / music playlist / long no-match matches' -ne 0 -or
         $run.Observations.'UI / music playlist / long near-match matches' -lt 1){throw 'Unexpected rendered music search results'}
      $reference=@($runs|Where-Object Status -eq 'Rendered')[0]
      foreach($query in @('long no-match','long near-match')) {
        foreach($suffix in @('matches','ordered ids hash','query length')) {
          $name='UI / music playlist / '+$query+' '+$suffix
          if($null -eq $run.Observations.$name -or $run.Observations.$name -ne $reference.Observations.$name){throw ('Changed music result: '+$name)}
        }
        if($run.Observations.('UI / music playlist / '+$query+' query length') -le 0){throw 'Missing music query length'}
      }
    }
  }
  return $runs
}
function Timing($runs,$name) {
  $values=foreach($run in $runs) {
    if($name -eq 'UI / music playlist / activation and render' -and
       $run.Observations.'UI / music playlist / painted Initial music view' -ne 1){continue}
    $read=$run.Phases|Where-Object Name -eq 'UI / music playlist / read and create incoming views'
    $phases=@($run.Phases|Where-Object {$_.Name -eq $name -and
      ($name -notin @('UI / music playlist / convert incoming views','UI / player playlist / create saved playlist views','UI / player playlist / collection publication','UI / music playlist / stored song database read','UI / music playlist / stored song enrichment publication','UI / player playlist / replace collection','UI / player playlist / dispatch active item') -or
       $_.StartedMilliseconds -ge $read.CompletedMilliseconds)})
    if($phases.Count -gt 1){throw ('Ambiguous music phase: '+$name)}
    if($phases.Count -eq 1 -and $phases[0].Milliseconds -gt 0){$phases[0].Milliseconds}
  }
  $sorted=@($values|Sort-Object)
  if(!$sorted.Count){return $null}
  $middle=[int][Math]::Floor($sorted.Count/2)
  if($sorted.Count%2){return $sorted[$middle]}
  return ($sorted[$middle-1]+$sorted[$middle])/2
}
$a=Read-Series $Baseline
$b=Read-Series $Optimized
if($b.Count){Same $a[0] $b[0]}
$c=@(Get-Content -LiteralPath $EnrichmentBaseline -Raw|ConvertFrom-Json)
if(!$c.Count){throw 'Empty enrichment control'}
foreach($run in $c){
  Same $a[0] $run
  if($run.Commit -notmatch '^[0-9a-fA-F]{40}$' -or $run.Commit -ne $c[0].Commit -or
     $run.Status -ne 'Timeout' -or $run.TimeoutSeconds -ne 60 -or $run.FailureType -or
     $run.DiagnosticMode -ne 'buffered-v1' -or $run.DiagnosticProfile -ne 'none' -or
     'UI / music playlist / stored song enrichment publication' -notin $run.ActivePhases){throw 'Invalid enrichment control'}
}
$scenarios=@(
  @{Name='load and render';Phase='UI / music playlist / load and render';Missing='Timeout (60k ms limit)'},
  @{Name='stored song enrichment';Phase='UI / music playlist / stored song enrichment publication';Missing='Unfinished';Control=$true},
  @{Name='collection publication';Phase='UI / player playlist / collection publication';Missing='Unfinished'},
  @{Name='collection replacement';Phase='UI / player playlist / replace collection';Missing='Not reached';Control=$true},
  @{Name='incoming song view conversion';Phase='UI / music playlist / convert incoming views';Missing='Not reached'},
  @{Name='saved playlist view creation';Phase='UI / player playlist / create saved playlist views';Missing='Not reached'},
  @{Name='active item dispatch';Phase='UI / player playlist / dispatch active item';Missing='Not reached';Control=$true},
  @{Name='database and incoming views';Phase='UI / music playlist / read and create incoming views';Missing='Not reached'},
  @{Name='stored song read';Phase='UI / music playlist / stored song database read';Missing='Not reached';Control=$true},
  @{Name='activation and render';Phase='UI / music playlist / activation and render';Missing='Not measured (painted)'},
  @{Name='scroll to last track';Phase='UI / music playlist / scroll to last track';Missing='Not reached'},
  @{Name='long no-match search and render';Phase='UI / music playlist / long no-match search and render';Missing='Not reached'},
  @{Name='long near-match search and render';Phase='UI / music playlist / long near-match search and render';Missing='Not reached'}
)
$lines=@('# 100k-entry music playlist','','Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.','','| Baseline now | New optimized version |','| --- | --- |')
foreach($scenario in $scenarios) {
  $old=Timing $(if($scenario.Control){$c}else{$a}) $scenario.Phase
  $new=if($b.Count){Timing $b $scenario.Phase}else{$null}
  $left=if($null -ne $old){Format-Time $old}else{$scenario.Missing}
  $right=if($null -ne $new){(Format-Time $new)+(Format-Change $old $new)}elseif($b.Count){$scenario.Missing}else{'Pending'}
  $lines+='| **'+$scenario.Name+'** — '+$left+' | '+$right+' |'
}
$lines+=@('','The timeout is the overall process limit. Unfinished endpoints have no percentage; completed phase medians exclude the small startup playlist. Stored-song/replacement/dispatch rows use the separate pre-lookup control. Painted activation has no prior baseline. Raw phases, failures and provenance stay in JSON.')
$lines|Set-Content -LiteralPath $Output
Write-Output ('Wrote '+$Output)
