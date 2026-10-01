param(
  [Parameter(Mandatory=$true)][string]$Baseline,
  [string]$Optimized,
  [string]$Output='performance/results.md'
)
$ErrorActionPreference='Stop'
$baselineData=Get-Content (Join-Path $Baseline 'features.json') -Raw | ConvertFrom-Json
$optimizedData=$null
if($Optimized) {
  $optimizedData=Get-Content (Join-Path $Optimized 'features.json') -Raw | ConvertFrom-Json
  foreach($field in @('FixtureSha256','Runtime','Configuration','ProcessorCount','OS')) {
    if($baselineData.$field -ne $optimizedData.$field) {throw ('Incompatible runs: '+$field)}
  }
}
function Format-Metric($metric) {
  if(!$metric) {return 'Pending'}
  return ('{0:N2} ms median; {1:N2} ms p95; {2:N2} ms first; {3:N1} MiB allocated' -f $metric.MedianMilliseconds,$metric.P95Milliseconds,$metric.FirstMilliseconds,($metric.MedianAllocatedBytes/1MB))
}
function Read-Startup($directory) {
  if(!$directory) {return @()}
  return @(Get-ChildItem -LiteralPath $directory -Filter 'startup-*.json' | ForEach-Object {Get-Content $_.FullName -Raw | ConvertFrom-Json})
}
function Format-Startup($runs,$name) {
  if(!$runs.Count) {return 'Pending'}
  $failed=@($runs | Where-Object Status -ne 'Rendered')
  if($failed.Count -and $name -eq "Application / first window render") {return ('Failed in {0}/{1} launches ({2}); no valid timing' -f $failed.Count,$runs.Count,(($failed | ForEach-Object {$_.Status+': '+$_.FailureType} | Select-Object -Unique) -join ', '))}
  $values=@($runs | ForEach-Object {$_.Phases | Where-Object Name -eq $name | ForEach-Object Milliseconds} | Sort-Object)
  if(!$values.Count) {return 'Incomplete or not reached'}
  return ('{0:N2} ms median; {1:N2} ms max; {2} launches' -f $values[[int][Math]::Floor($values.Count/2)],$values[-1],$runs.Count)
}
$lines=[Collections.Generic.List[string]]::new()
$lines.Add('# Worst-case performance comparison')
$lines.Add('')
$lines.Add('Baseline commit: '+$baselineData.Commit+'. Optimized commit: '+$(if($optimizedData){$optimizedData.Commit}else{'pending'})+'.')
$lines.Add('Workload: '+$baselineData.Fixture.SoundItems+' sound items, '+$baselineData.Fixture.Playlists+' playlists, plus 1,000 / 10,000 / 100,000-entry stress playlists. Release x64 on .NET '+$baselineData.Runtime+'.')
$lines.Add('Fixture SHA-256: '+$baselineData.FixtureSha256+'. Baseline samples are immutable.')
$lines.Add('')
$lines.Add('## Application startup')
$lines.Add('')
$lines.Add('| Baseline now | New optimized version |')
$lines.Add('| --- | --- |')
$baseStartup=Read-Startup $Baseline
$newStartup=Read-Startup $Optimized
$startupNames=@('Application / first window render','Application / initialization','Application / shell construction','Application / module registration','Application / container activation','Application / settings')
$startupNames+=@($baseStartup | ForEach-Object {$_.Phases | ForEach-Object Name} | Where-Object {$_ -notin $startupNames} | Select-Object -Unique)
foreach($name in $startupNames) {
  $lines.Add('| **'+$name+'** — '+(Format-Startup $baseStartup $name)+' | '+(Format-Startup $newStartup $name)+' |')
}
foreach($category in @('Data','Playlist','UI','Lyrics','Spectrum')) {
  $lines.Add('')
  $lines.Add('## '+$category)
  $lines.Add('')
  $lines.Add('| Baseline now | New optimized version |')
  $lines.Add('| --- | --- |')
  # Sort biggest to smallest within each category.
  foreach($metric in @($baselineData.Metrics | Where-Object Category -eq $category | Sort-Object MedianMilliseconds -Descending)) {
    $new=$null
    if($optimizedData) {
      $new=$optimizedData.Metrics | Where-Object Name -eq $metric.Name
      if(!$new -or $new.Boundary -ne $metric.Boundary -or $new.Workload -ne $metric.Workload) {throw ('Changed or missing metric boundary: '+$metric.Name)}
    }
    $lines.Add('| **'+$metric.Name+'** — '+(Format-Metric $metric)+' | '+(Format-Metric $new)+' |')
  }
}
$lines.Add('')
$lines.Add('## Coverage still requiring end-to-end scenarios')
$lines.Add('')
$lines.Add('| Baseline now | New optimized version |')
$lines.Add('| --- | --- |')
foreach($feature in @('Library card templates and scrolling','Navigation and detail views','File-browser folders and thumbnails','Settings and modal dialogs','Video and fullscreen transitions','Cloud and network timeout handling','LibraryCollection full load/filter publication')) {
  $lines.Add('| **'+$feature+'** — Not measured yet | Pending |')
}
$lines.Add('')
$lines.Add('Each feature metric is one operation except lyrics (10,000 seeks) and spectrum (1,000 frames). Divide batched results by their operation count before ranking across categories. UI list scenarios use an offscreen text-row ListBox, not application card templates. First samples share one process, so only the first metric includes process-cold EF initialization. Startup uses fresh processes with warm OS caches and fresh default settings; it does not include every background service becoming ready. Startup phase scopes overlap. Allocation counts cover managed allocations across threads and exclude native bitmap/database memory.')
$lines.Add('')
$lines.Add('See README.md for workload boundaries and the optimization protocol.')
$lines | Set-Content -LiteralPath $Output
Write-Output ('Wrote '+$Output)
