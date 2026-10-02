param(
  [string]$Baseline=(Join-Path $PSScriptRoot 'playlist-preparation-baseline.json'),
  [string]$Candidate=(Join-Path $PSScriptRoot 'playlist-preparation-candidate.json'),
  [string]$Output=(Join-Path $PSScriptRoot 'playlist-preparation-results.md')
)
$ErrorActionPreference='Stop'
$phases=@(
  'Application / initial playlist view ready',
  'Application / first window render',
  'Application / library SoundItemFilePlaylist / query'
)
$a=@(Get-Content -LiteralPath $Baseline -Raw|ConvertFrom-Json)
$b=@(Get-Content -LiteralPath $Candidate -Raw|ConvertFrom-Json)
if(!$a.Count -or !$b.Count -or $a.Count -ne $b.Count){throw 'Preparation comparison requires balanced nonempty series'}
if($a[0].Commit -ne $b[0].Commit){throw 'Preparation comparison requires the same compiled commit'}
foreach($series in @(@{Runs=$a;Prepared=$false},@{Runs=$b;Prepared=$true})) {
  foreach($run in $series.Runs) {
    if($run.PlaylistQueryPreparation -isnot [bool] -or $run.PlaylistQueryPreparation -ne $series.Prepared){throw 'Missing or mixed preparation mode'}
    if($run.Status -ne 'Rendered' -or $run.WindowMode -ne 'Visible'){throw 'Preparation comparison requires rendered visible launches'}
    if($run.DiagnosticMode -ne 'buffered-v1' -or $run.DiagnosticProfile -ne 'none'){throw 'Preparation comparison excludes CPU/trace overhead'}
    foreach($phase in $phases) {
      $matches=@($run.Phases|Where-Object Name -eq $phase)
      if($matches.Count -ne 1 -or $matches[0].Milliseconds -le 0){throw ('Missing or invalid phase: '+$phase)}
    }
    $preparation=@($run.Phases|Where-Object Name -eq 'Application / playlist query preparation')
    if($preparation.Count -ne [int]$series.Prepared){throw 'Preparation phase does not match mode'}
    foreach($name in @('UI / grouped playlist items','UI / realized playlist rows')) {
      if($run.Observations.$name -le 0 -or $run.Observations.$name -ne $a[0].Observations.$name){throw ('Changed or empty rendered workload: '+$name)}
    }
  }
}
& (Join-Path $PSScriptRoot 'ui-report.ps1') -Baseline $Baseline -Optimized $Candidate -Output $Output -PhaseNames $phases
$content=Get-Content -LiteralPath $Output -Raw
$content=$content.Replace('# Buffered UI performance','# Playlist query preparation experiment')
$content=$content.Replace('| Baseline now | New optimized version |','| Baseline now | Optional candidate |')
$content=$content.Replace('library SoundItemFilePlaylist / query','playlist data query')
$content += [Environment]::NewLine+'Preparation stays disabled by default: the large query reduction produced a small readiness gain and a slower first render. Both series retain their slow outliers.'+[Environment]::NewLine
Set-Content -LiteralPath $Output -Value $content -NoNewline
