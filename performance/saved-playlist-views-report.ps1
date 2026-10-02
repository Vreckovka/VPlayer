param(
  [Parameter(Mandatory=$true)][string]$Baseline,
  [Parameter(Mandatory=$true)][string]$Optimized,
  [string]$Output=(Join-Path $PSScriptRoot 'saved-playlist-views-results.md')
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
function Read-Runs([string]$directory) {
  if(Test-Path -LiteralPath $directory -PathType Leaf){
    $runs=@(Get-Content -LiteralPath $directory -Raw | ConvertFrom-Json)
  } else {
    $files=@(Get-ChildItem -LiteralPath $directory -File -Filter 'saved-views-*.json' | Sort-Object Name)
    $runs=@($files | ForEach-Object {Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json})
  }
  if($runs.Count -ne 3){throw 'Requires three independent saved view processes.'}
  foreach($run in $runs){
    if($run.SchemaVersion -ne 1 -or $run.Entries -ne 100000 -or $run.Configuration -ne 'Release' -or $run.Commit -notmatch '^[0-9a-f]{40}$' -or $run.Samples.Count -ne 5){throw 'Invalid saved view run.'}
    for($i=0;$i -lt 5;$i++){
      $sample=$run.Samples[$i]
      if($sample.Iteration -ne $i -or $sample.Milliseconds -le 0 -or $sample.AllocatedBytes -le 0){throw 'Invalid saved view samples.'}
    }
    foreach($field in @('Commit','FixtureSha256','Runtime','ProcessorCount','Boundary','MissingFileInfo')){
      if($null -eq $run.$field -or $run.$field -ne $runs[0].$field){throw ('Mixed saved view controls: '+$field)}
    }
  }
  return $runs
}
function Format-Memory($bytes){
  if($bytes -ge 1GB){return ($bytes/1GB).ToString('0.#',[Globalization.CultureInfo]::InvariantCulture)+' GiB'}
  return ($bytes/1MB).ToString('0.#',[Globalization.CultureInfo]::InvariantCulture)+' MiB'
}
function Median($values){$sorted=@($values | Sort-Object);$middle=[int][math]::Floor($sorted.Count/2);if($sorted.Count%2){return $sorted[$middle]};return ($sorted[$middle-1]+$sorted[$middle])/2}
$a=@(Read-Runs $Baseline)
$b=@(Read-Runs $Optimized)
foreach($field in @('FixtureSha256','Runtime','ProcessorCount','Boundary','MissingFileInfo')){if($a[0].$field -ne $b[0].$field){throw ('Incompatible baseline: '+$field)}}
$firstA=Median @($a | ForEach-Object {$_.Samples[0].Milliseconds})
$firstB=Median @($b | ForEach-Object {$_.Samples[0].Milliseconds})
$repeatA=Median @($a | ForEach-Object {$_.Samples | Select-Object -Skip 1 | ForEach-Object {$_.Milliseconds}})
$repeatB=Median @($b | ForEach-Object {$_.Samples | Select-Object -Skip 1 | ForEach-Object {$_.Milliseconds}})
$allocA=Median @($a | ForEach-Object {$_.Samples | ForEach-Object {$_.AllocatedBytes}})
$allocB=Median @($b | ForEach-Object {$_.Samples | ForEach-Object {$_.AllocatedBytes}})
$lines=@(
  '# Saved playlist view creation',
  '',
  '100k stored entries; real Ninject, eight shared stub services and a separate WindowManager per entry. Component timings exclude database reads, rendering, verification and disposal.',
  '',
  '| Baseline now | New optimized version |',
  '| --- | --- |',
  ('| **first batch** — '+(Format-Time $firstA)+' | '+(Format-Time $firstB)+(Format-Change $firstA $firstB)+' |'),
  ('| **repeated batch** — '+(Format-Time $repeatA)+' | '+(Format-Time $repeatB)+(Format-Change $repeatA $repeatB)+' |'),
  ('| **allocated per batch** — '+(Format-Memory $allocA)+' | '+(Format-Memory $allocB)+(Format-Change $allocA $allocB)+' |'),
  '',
  ('Source commits: `'+$a[0].Commit.Substring(0,8)+'` → `'+$b[0].Commit.Substring(0,8)+'`. Original samples and validation remain in the raw JSON.')
)
$lines | Set-Content -LiteralPath $Output -Encoding UTF8
