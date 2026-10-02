param(
  [Parameter(Mandatory=$true)][string]$Baseline,
  [string]$Optimized,
  [string]$StartupOptimized,
  [string]$ReadyBaseline='performance/startup-ready-baseline.json',
  [string[]]$StartupPhaseBaselines=@('performance/startup-detail-baseline.json','performance/startup-shell-baseline.json'),
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
  $values=@($runs | ForEach-Object {$_.Phases | Where-Object Name -eq $name | ForEach-Object Milliseconds} | Sort-Object)
  if(!$values.Count) {
    $failures=@($runs | Where-Object Status -ne 'Rendered')
    if($failures.Count) {
      return ('Not reached; {0}/{1} failed launches ({2})' -f $failures.Count,$runs.Count,(($failures | ForEach-Object {$_.Status+': '+$_.FailureType} | Select-Object -Unique) -join ', '))
    }
    return 'Not instrumented or not reached'
  }
  $formatted=('{0:N2} ms median; {1:N2} ms max; reached in {2}/{3} launches' -f $values[[int][Math]::Floor($values.Count/2)],$values[-1],$values.Count,$runs.Count)
  if($values.Count -ne $runs.Count) {$formatted+='; incomplete sample set'}
  return $formatted
}
function Startup-Median($runs,$name) {
  $values=@($runs | ForEach-Object {$_.Phases | Where-Object Name -eq $name | ForEach-Object Milliseconds} | Sort-Object)
  if(!$values.Count) {return -1}
  return $values[[int][Math]::Floor($values.Count/2)]
}
$baseStartup=Read-Startup $Baseline
$newStartup=Read-Startup $(if($StartupOptimized){$StartupOptimized}else{$Optimized})
$readyStartup=@()
if($ReadyBaseline -and (Test-Path -LiteralPath $ReadyBaseline)) {$readyStartup=@(Get-Content $ReadyBaseline -Raw | ConvertFrom-Json)}
$phaseSources=[Collections.Generic.List[object]]::new()
foreach($path in $StartupPhaseBaselines) {
  if(Test-Path -LiteralPath $path) {$phaseSources.Add(@(Get-Content $path -Raw | ConvertFrom-Json))}
}
function Startup-Reference($name) {
  if($name -eq 'Application / initial playlist view ready') {return $readyStartup}
  if($name -eq 'Application / first window render') {return $baseStartup}
  if(@($baseStartup | ForEach-Object {$_.Phases | Where-Object Name -eq $name}).Count) {return $baseStartup}
  foreach($runs in $phaseSources) {
    if(@($runs | ForEach-Object {$_.Phases | Where-Object Name -eq $name}).Count) {return $runs}
  }
  return $baseStartup
}
foreach($runs in (@($baseStartup,$newStartup,$readyStartup)+@($phaseSources))) {
  if($runs.Count -and @($runs | ForEach-Object WindowMode | Select-Object -Unique).Count -ne 1) {throw 'Mixed startup window modes'}
  if($runs.Count -and @($runs | ForEach-Object Commit | Select-Object -Unique).Count -ne 1) {throw 'Mixed startup commits'}
}
if($newStartup.Count -and $baseStartup.Count -and $newStartup[0].WindowMode -ne $baseStartup[0].WindowMode) {throw 'Incompatible startup window modes'}
if($newStartup.Count -and $readyStartup.Count -and $newStartup[0].WindowMode -ne $readyStartup[0].WindowMode) {throw 'Incompatible readiness window modes'}
$lines=[Collections.Generic.List[string]]::new()
$lines.Add('# Worst-case performance comparison')
$lines.Add('')
$lines.Add('Baseline commit: '+$baselineData.Commit+'. Optimized commit: '+$(if($optimizedData){$optimizedData.Commit}else{'pending'})+'.')
$lines.Add('Workload: '+$baselineData.Fixture.SoundItems+' sound items, '+$baselineData.Fixture.Playlists+' playlists, plus 1,000 / 10,000 / 100,000-entry stress playlists. Release x64 on .NET '+$baselineData.Runtime+'.')
$lines.Add('Fixture SHA-256: '+$baselineData.FixtureSha256+'. Baseline samples are immutable.')
$lines.Add('')
$lines.Add('## Application startup')
$lines.Add('')
$lines.Add('Startup comparison commit: '+$(if($newStartup.Count){$newStartup[0].Commit}else{'pending'})+'.')
if($readyStartup.Count) {
  $lines.Add('The initial populated-view milestone has a separate baseline at '+$readyStartup[0].Commit+' after repairing the startup crash. Earlier failed launches cannot provide this timing.')
}
if($phaseSources.Count) {
  $lines.Add('Additional startup phases use the first recorded baseline containing that phase. Baseline commits: '+(($phaseSources | ForEach-Object {$_.Commit} | Select-Object -Unique) -join ', ')+'. These were recorded before query deferral.')
}
$startupNames=@($baseStartup | ForEach-Object {$_.Phases | ForEach-Object Name})
$startupNames+=@($phaseSources | ForEach-Object {$_ | ForEach-Object {$_.Phases | ForEach-Object Name}})
$startupNames+=@($newStartup | ForEach-Object {$_.Phases | ForEach-Object Name})
$startupNames+=@('Application / first window render')
$startupNames=@($startupNames | Select-Object -Unique | Where-Object {$_ -ne 'Application / initial playlist view ready'} | Sort-Object {Startup-Median (Startup-Reference $_) $_} -Descending)
$startupNames=@('Application / first window render')+@($startupNames | Where-Object {$_ -ne 'Application / first window render'})
if($readyStartup.Count) {$startupNames=@('Application / initial playlist view ready')+$startupNames}
# Put explanatory prose before the table, preserving exactly two table columns.
$lines.Add('')
$lines.Add('| Baseline now | New optimized version |')
$lines.Add('| --- | --- |')
foreach($name in $startupNames) {
  $reference=@(Startup-Reference $name)
  $lines.Add('| **'+$name+'** — '+(Format-Startup $reference $name)+' | '+(Format-Startup $newStartup $name)+' |')
  if($phaseSources.Count -and $name -in @('Application / first window render','Application / initial playlist view ready')) {
    $latestReference=@($phaseSources[$phaseSources.Count-1])
    $lines.Add('| **'+$name+' before query deferral** — '+(Format-Startup $latestReference $name)+' | '+(Format-Startup $newStartup $name)+' |')
  }
  if($name -eq 'Application / first window render' -and $readyStartup.Count) {
    $lines.Add('| **Application / first window render after crash fix** — '+(Format-Startup $readyStartup $name)+' | '+(Format-Startup $newStartup $name)+' |')
  }
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
    $newText=Format-Metric $new
    if($new -and $metric.MedianMilliseconds -gt 0) {
      $change=100*($metric.MedianMilliseconds-$new.MedianMilliseconds)/$metric.MedianMilliseconds
      $newText+=('; {0:N1}% {1}' -f [Math]::Abs($change),$(if($change -ge 0){'faster'}else{'slower'}))
    }
    $lines.Add('| **'+$metric.Name+'** — '+(Format-Metric $metric)+' | '+$newText+' |')
  }
  if($category -eq 'UI') {
    $groupedReport=Join-Path $PSScriptRoot 'grouped-ui-results.md'
    if(Test-Path -LiteralPath $groupedReport) {
      $lines.Add('')
      $lines.Add('### Grouped music playlists — actual application')
      $lines.Add('')
      $lines.Add('Separate worst-case fixture: 207,110 sound items and 5,310 playlists, including 5,000 additional favorites with long titles. Seven visible fresh-profile launches per version. The baseline failed to become usable within 60 seconds; times from its first frame are partial observations.')
      $lines.Add('')
      foreach($row in @(Get-Content -LiteralPath $groupedReport | Where-Object { $_.StartsWith('|') })) {$lines.Add($row)}
      $lines.Add('')
      $lines.Add('See [grouped-ui-results.md](grouped-ui-results.md) for fixture identity, benchmark boundaries and complete scenario notes.')
    }
  }
}
$lines.Add('')
$lines.Add('## Coverage still requiring end-to-end scenarios')
$lines.Add('')
$lines.Add('| Baseline now | New optimized version |')
$lines.Add('| --- | --- |')
foreach($feature in @('Library card templates and scrolling','Navigation and detail views','File-browser folders and thumbnails','Settings and modal dialogs','Video and fullscreen transitions','Cloud and network timeout handling','LibraryCollection full sound-item load/fuzzy filter publication')) {
  $lines.Add('| **'+$feature+'** — Not measured yet | Pending |')
}
$lines.Add('')
$lines.Add('This comparison includes playlist loading, the startup crash repair, library query deferral, and actual grouped-playlist UI virtualization. Other feature metrics are controls: timing differences in unchanged code are observations and must not be credited to those changes.')
$lines.Add('')
$lines.Add('Each feature metric is one operation except lyrics (10,000 seeks) and spectrum (1,000 frames). Divide batched results by their operation count before ranking across categories. Component UI list scenarios use an offscreen text-row ListBox, not application card templates. First samples share one process, so only the first metric includes process-cold EF initialization. Startup uses fresh processes with warm OS caches and fresh default settings; it does not include every background service becoming ready. Startup phase scopes overlap. Allocation counts cover managed allocations across threads and exclude native bitmap/database memory.')
$lines.Add('')
$lines.Add('See README.md for workload boundaries and the optimization protocol.')
$lines | Set-Content -LiteralPath $Output
Write-Output ('Wrote '+$Output)
