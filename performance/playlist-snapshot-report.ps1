param(
  [Parameter(Mandatory=$true)][string]$Baseline,
  [Parameter(Mandatory=$true)][string]$Optimized,
  [string]$Output=(Join-Path $PSScriptRoot 'playlist-snapshot-results.md')
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
function Read-Runs([string]$directory) {
  $files=@(Get-ChildItem -LiteralPath $directory -File -Filter 'snapshot-*.json' | Sort-Object Name)
  if($files.Count -ne 3){throw 'Requires three independent snapshot processes.'}
  $runs=@($files | ForEach-Object {Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json})
  foreach($run in $runs){
    if($run.Entries -ne 100000 -or $run.Configuration -ne 'Release' -or $run.Commit -notmatch '^[0-9a-f]{40}$' -or $run.Samples.Count -ne 5){throw 'Invalid snapshot run.'}
    for($i=0;$i -lt 5;$i++){
      $sample=$run.Samples[$i]
      if($sample.Iteration -ne $i -or $sample.Milliseconds -le 0 -or $sample.AllocatedBytes -le 0){throw 'Invalid snapshot samples.'}
    }
    foreach($field in @('Commit','FixtureSha256','Runtime','ProcessorCount','Boundary')){
      if($run.$field -ne $runs[0].$field){throw ('Mixed snapshot controls: '+$field)}
    }
  }
  return $runs
}
function Median($values){$sorted=@($values | Sort-Object);$middle=[int][math]::Floor($sorted.Count/2);if($sorted.Count%2){return $sorted[$middle]};return ($sorted[$middle-1]+$sorted[$middle])/2}
$a=@(Read-Runs $Baseline)
$b=@(Read-Runs $Optimized)
foreach($field in @('FixtureSha256','Runtime','ProcessorCount','Boundary')){if($a[0].$field -ne $b[0].$field){throw ('Incompatible baseline: '+$field)}}
$firstA=Median @($a | ForEach-Object {$_.Samples[0].Milliseconds})
$firstB=Median @($b | ForEach-Object {$_.Samples[0].Milliseconds})
$repeatA=Median @($a | ForEach-Object {$_.Samples | Select-Object -Skip 1 | ForEach-Object {$_.Milliseconds}})
$repeatB=Median @($b | ForEach-Object {$_.Samples | Select-Object -Skip 1 | ForEach-Object {$_.Milliseconds}})
$allocA=Median @($a | ForEach-Object {$_.Samples | ForEach-Object {$_.AllocatedBytes}})
$allocB=Median @($b | ForEach-Object {$_.Samples | ForEach-Object {$_.AllocatedBytes}})
$lines=@(
  '# Playlist snapshot',
  '',
  '100k stored entries. Component timings; database work, UI rendering and media playback are excluded.',
  '',
  '| Baseline now | New optimized version |',
  '| --- | --- |',
  ('| **first snapshot** — '+(Format-Time $firstA)+' | '+(Format-Time $firstB)+(Format-Change $firstA $firstB)+' |'),
  ('| **repeated snapshot** — '+(Format-Time $repeatA)+' | '+(Format-Time $repeatB)+(Format-Change $repeatA $repeatB)+' |'),
  ('| **allocated per snapshot** — '+[math]::Round($allocA/1MB,1)+' MiB | '+[math]::Round($allocB/1MB,1)+' MiB ('+(Format-Change $allocA $allocB)+') |'),
  '',
  ('Source commits: `'+$a[0].Commit.Substring(0,8)+'` → `'+$b[0].Commit.Substring(0,8)+'`. Original samples and validation remain in the raw JSON.')
)
$lines | Set-Content -LiteralPath $Output -Encoding UTF8
