param(
  [string]$Baseline='performance/music-playlist-write-baseline.json',
  [string]$Optimized='performance/music-playlist-write-optimized.json',
  [string]$Output='performance/music-playlist-write-results.md'
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
$baselineData=Get-Content -LiteralPath $Baseline -Raw | ConvertFrom-Json
$optimizedData=Get-Content -LiteralPath $Optimized -Raw | ConvertFrom-Json
foreach($data in @($baselineData,$optimizedData)) {
  $commits=@(@($data.Save)+@($data.Clear) | ForEach-Object Commit | Select-Object -Unique)
  if($commits.Count -ne 1 -or $commits[0] -notmatch '^[0-9a-f]{40}$'){throw 'Mixed or missing write-series commits'}
}
function Read-WriteSeries($data,[string]$kind,[bool]$allowTimeout) {
  $runs=@($data.$kind)
  if(!$runs.Count){throw "Missing $kind series"}
  if(@($runs.Commit | Select-Object -Unique).Count -ne 1){throw 'Mixed commits in a series'}
  foreach($run in $runs) {
    if($run.WindowMode -ne 'Visible' -or $run.ProcessTimeoutSeconds -ne 180 -or
      $run.MusicPlaylistEntries -ne 100000 -or $run.DiagnosticMode -ne 'buffered-v1' -or
      $run.DiagnosticProfile -ne 'none' -or $run.FailureType){throw 'Invalid write workload or protocol'}
    if([bool]$run.MusicPlaylistSave -ne ($kind -eq 'Save') -or
      [bool]$run.MusicPlaylistClear -ne ($kind -eq 'Clear')){throw 'Wrong write operation'}
    if($run.Observations.'UI / music playlist / write rows before' -ne 100000 -or
      $run.Observations.'UI / music playlist / ordered occurrence check' -ne 1 -or
      $run.Observations.'UI / music playlist / painted 100k music write workload' -ne 1){throw 'Write workload was not ready'}
    if($run.Status -eq 'Timeout' -and $allowTimeout) {
      if($run.TimeoutSeconds -ne 180 -or
        $run.ActivePhases -notcontains "UI / music playlist / $($kind.ToLowerInvariant()) 100k and render") {
        throw 'Timeout did not reach the write endpoint'
      }
      continue
    }
    if($run.Status -ne 'Rendered' -or @($run.ActivePhases | Where-Object {$_.StartsWith('UI / music playlist /')}).Count){throw 'Write endpoint did not complete'}
    if($run.Observations.'UI / music playlist / persisted write occurrence check' -ne 1 -or
      $run.Observations.'UI / music playlist / persisted write rows' -ne 100000){throw 'Persisted rows were not verified'}
    $paint=if($kind -eq 'Save'){'Saved'}else{'Cleared'}
    $count=if($kind -eq 'Save'){100000}else{0}
    if($run.Observations."UI / music playlist / painted $paint 100k music playlist" -ne 1 -or
      $run.Observations."UI / music playlist / $($paint.ToLowerInvariant()) rows after" -ne $count){throw 'Missing painted write result'}
    $phase=@($run.Phases | Where-Object Name -eq "UI / music playlist / $($kind.ToLowerInvariant()) 100k and render")
    if($phase.Count -ne 1 -or $phase[0].Milliseconds -le 0){throw 'Missing write endpoint timing'}
  }
  return $runs
}
function Write-Median($runs,[string]$kind) {
  if(@($runs | Where-Object Status -ne 'Rendered').Count){return $null}
  $values=@($runs | ForEach-Object {$_.Phases | Where-Object Name -eq "UI / music playlist / $($kind.ToLowerInvariant()) 100k and render" | ForEach-Object Milliseconds} | Sort-Object)
  return $values[[int][Math]::Floor($values.Count/2)]
}
$lines=[Collections.Generic.List[string]]::new()
$lines.Add('# 100k-track save and clear')
$lines.Add('')
$lines.Add('Actual WPF operation through its painted result, with every persisted occurrence checked. Initial baseline versus optimized median; negative percentages mean less time.')
$lines.Add('')
$lines.Add('| Baseline now | New optimized version |')
$lines.Add('| --- | --- |')
foreach($kind in @('Save','Clear')) {
  $reference=Read-WriteSeries $baselineData $kind $true
  $current=Read-WriteSeries $optimizedData $kind $false
  foreach($run in @($reference)+@($current)) {
    foreach($field in @('FixtureSha256','Runtime','Configuration','ProcessorCount','OS','ProcessTimeoutSeconds')) {
      if($run.$field -ne $reference[0].$field){throw "Incompatible write series: $field"}
    }
  }
  $before=Write-Median $reference $kind
  $after=Write-Median $current $kind
  $baseText=if($null -eq $before){'Timed out'}else{Format-Time $before}
  $label=if($kind -eq 'Save'){'Reorder, save and render 100k tracks'}else{'Save, clear and render 100k tracks'}
  $lines.Add('| **'+$label+'** — '+$baseText+' | '+(Format-Time $after)+(Format-Change $before $after)+' |')
}
$lines.Add('')
if(@($baselineData.Save | Where-Object Status -ne 'Rendered').Count) {
  $lines.Add('The save baseline did not complete; it has no percentage.')
}
$lines.Add('Raw JSON retains all phases, failures and precision, including shutdown save scopes after the verified endpoint. Timings include queue waits and background activity on this machine.')
$lines.Add('')
$lines.Add('Earlier rerun failures remain in [retained evidence](iterations/music-playlist-metadata-native-attempts-074136ae.json): initial-render timeout and playback-save exception. Completed timings do not establish failure rates.')
$lines.Add('')
$lines.Add('Baseline: `'+$baselineData.Save[0].Commit+'`; optimized: `'+$optimizedData.Save[0].Commit+'`.')
[IO.File]::WriteAllLines((Join-Path $PWD $Output),$lines,(New-Object Text.UTF8Encoding $false))