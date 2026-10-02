param(
  [Parameter(Mandatory=$true)][string]$Baseline,
  [string]$Optimized,
  [string]$StartupOptimized,
  [string]$ReadyBaseline='performance/startup-ready-baseline.json',
  [string[]]$StartupPhaseBaselines=@('performance/startup-detail-baseline.json','performance/startup-shell-baseline.json'),
  [string]$Output='performance/results.md'
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
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
  return Format-Time $metric.MedianMilliseconds
}
function Read-Startup($directory) {
  if(!$directory) {return @()}
  return @(Get-ChildItem -LiteralPath $directory -Filter 'startup-*.json' | ForEach-Object {Get-Content $_.FullName -Raw | ConvertFrom-Json})
}
function Format-Startup($runs,$name) {
  if(!$runs.Count) {return 'Pending'}
  $value=Startup-Median $runs $name
  if($value -lt 0) {
    if($name -eq 'Application / first window render' -and @($runs | Where-Object Status -ne 'Rendered').Count) {return 'Failed'}
    return 'n/a'
  }
  $text=Format-Time $value
  $reached=@($runs | Where-Object {$_.Phases.Name -contains $name}).Count
  if($reached -ne $runs.Count) {$text+=' (partial)'}
  return $text
}
function Startup-Comparison($reference,$runs,$name) {
  Assert-DiagnosticMode $reference $runs
  return (Format-Startup $runs $name)+(Format-Change (Startup-Median $reference $name) (Startup-Median $runs $name))
}
function Display-Name([string]$name) {
  $name=$name -replace '^(Application|Data|Playlist|UI|Lyrics|Spectrum) / ',''
  $name=$name.Replace('library SoundItemFilePlaylist / ','Playlists / ')
  $names=@{
    SoundItemPlaylistsViewModel='Music playlists';IArtistsViewModel='Artists';IAlbumsViewModel='Albums'
    WindowsFileBrowserViewModel='File browser';SettingsViewModel='Settings';PCloudManagerViewModel='Cloud'
    VideoPlaylistsViewModel='Video playlists';TvShowsViewModel='TV shows';StatisticsViewModel='Statistics'
    UPnPManagerViewModel='UPnP'
  }
  foreach($key in $names.Keys) {$name=$name.Replace('library '+$key+' / navigation construction',$names[$key]+' / construction')}
  return $name
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
  Assert-DiagnosticMode $runs @()
  if($runs.Count -and @($runs | ForEach-Object WindowMode | Select-Object -Unique).Count -ne 1) {throw 'Mixed startup window modes'}
  if($runs.Count -and @($runs | ForEach-Object Commit | Select-Object -Unique).Count -ne 1) {throw 'Mixed startup commits'}
}
if($newStartup.Count -and $baseStartup.Count -and $newStartup[0].WindowMode -ne $baseStartup[0].WindowMode) {throw 'Incompatible startup window modes'}
if($newStartup.Count -and $readyStartup.Count -and $newStartup[0].WindowMode -ne $readyStartup[0].WindowMode) {throw 'Incompatible readiness window modes'}
$lines=[Collections.Generic.List[string]]::new()
$lines.Add('# Worst-case performance')
$lines.Add('')
$lines.Add('207k sound items; stress playlists up to 100k entries. Median timings; **- % = less time**, **+ % = more time**. Failed or missing baselines have no percentage.')
$lines.Add('')
$lines.Add('## Application startup')
$lines.Add('')
$lines.Add('| Baseline now | New optimized version |')
$lines.Add('| --- | --- |')
foreach($name in @('Application / initial playlist view ready','Application / first window render')) {
  $reference=@(Startup-Reference $name)
  $lines.Add('| **'+(Display-Name $name)+'** — '+(Format-Startup $reference $name)+' | '+(Startup-Comparison $reference $newStartup $name)+' |')
}
if($readyStartup.Count) {
  $name='Application / first window render'
  $lines.Add('| **First render after crash fix** — '+(Format-Startup $readyStartup $name)+' | '+(Startup-Comparison $readyStartup $newStartup $name)+' |')
}
$lines.Add('')
$lines.Add('<details>')
$lines.Add('<summary>Startup breakdown</summary>')
$lines.Add('')
$lines.Add('| Baseline now | New optimized version |')
$lines.Add('| --- | --- |')
$startupNames=@($baseStartup | ForEach-Object {$_.Phases.Name})
$startupNames+=@($phaseSources | ForEach-Object {$_ | ForEach-Object {$_.Phases.Name}})
$startupNames+=@($newStartup | ForEach-Object {$_.Phases.Name})
$startupNames=@($startupNames | Select-Object -Unique | Where-Object {$_ -notin @('Application / initial playlist view ready','Application / first window render')} | Sort-Object {Startup-Median (Startup-Reference $_) $_} -Descending)
foreach($name in $startupNames) {
  $reference=@(Startup-Reference $name)
  $lines.Add('| **'+(Display-Name $name)+'** — '+(Format-Startup $reference $name)+' | '+(Startup-Comparison $reference $newStartup $name)+' |')
}
if($phaseSources.Count) {
  $reference=@($phaseSources[$phaseSources.Count-1])
  foreach($name in @('Application / initial playlist view ready','Application / first window render')) {
    $lines.Add('| **'+(Display-Name $name)+' before query deferral** — '+(Format-Startup $reference $name)+' | '+(Startup-Comparison $reference $newStartup $name)+' |')
  }
}
$lines.Add('')
$lines.Add('</details>')
foreach($category in @('Data','Playlist','UI','Lyrics','Spectrum')) {
  $lines.Add('')
  $title=switch($category){'Lyrics' {'Lyrics (10k seeks)'} 'Spectrum' {'Spectrum (1k frames)'} default {$category}}
  $lines.Add('## '+$title)
  $lines.Add('')
  $lines.Add('| Baseline now | New optimized version |')
  $lines.Add('| --- | --- |')
  foreach($metric in @($baselineData.Metrics | Where-Object Category -eq $category | Sort-Object MedianMilliseconds -Descending)) {
    $new=$null
    if($optimizedData) {
      $new=$optimizedData.Metrics | Where-Object Name -eq $metric.Name
      if(!$new -or $new.Boundary -ne $metric.Boundary -or $new.Workload -ne $metric.Workload) {throw ('Changed or missing metric boundary: '+$metric.Name)}
    }
    $newText=Format-Metric $new
    if($new) {$newText+=Format-Change $metric.MedianMilliseconds $new.MedianMilliseconds}
    $label=(Display-Name $metric.Name) -replace '100000','100k' -replace '10000','10k' -replace '1000','1k'
    $lines.Add('| **'+$label+'** — '+(Format-Metric $metric)+' | '+$newText+' |')
  }
  if($category -in @('Data','UI')) {
    foreach($scenario in @(
      @{File='statistics-results.md';Title='Statistics (207k unique file records)'},
      @{File='statistics-played-results.md';Title='Statistics (fully played, 207k unique times)'}
    )) {
      $scenarioReport=Join-Path $PSScriptRoot $scenario.File
      if(!(Test-Path -LiteralPath $scenarioReport)){continue}
      $content=@(Get-Content -LiteralPath $scenarioReport)
      $lines.Add('')
      $lines.Add('### '+$scenario.Title)
      $lines.Add('')
      $lines.Add('| Baseline now | New optimized version |')
      $lines.Add('| --- | --- |')
      $label=if($category -eq 'Data'){'data load**'}else{'**load and render**'}
      foreach($row in @($content|Where-Object {$_.StartsWith('|') -and $_.Contains($label)})) {$lines.Add($row)}
      if($category -eq 'UI') {
        foreach($note in @($content|Where-Object {$_.StartsWith('* UI timing')})) {$lines.Add('');$lines.Add($note)}
      }
    }
  }
  if($category -eq 'UI') {
    $groupedReport=Join-Path $PSScriptRoot 'grouped-ui-results.md'
    if(Test-Path -LiteralPath $groupedReport) {
      $lines.Add('')
      $lines.Add('### Grouped playlists (5k added favorites)')
      $lines.Add('')
      foreach($row in @(Get-Content -LiteralPath $groupedReport | Where-Object { $_.StartsWith('|') })) {$lines.Add($row)}
    }
  }
}
$lines.Add('')
$lines.Add('## Still to measure')
$lines.Add('')
$lines.Add('Library cards/scrolling; navigation/details; file browser/thumbnails; settings/dialogs; video/fullscreen; cloud timeouts; full library load/fuzzy filtering.')
$lines.Add('')
$lines.Add('Percentage changes in unchanged code are observations. Startup phases overlap; component UI tests use a simplified list. Raw samples, maxima, allocations and commit/fixture provenance remain in the JSON files and [README](README.md).')
$lines | Set-Content -LiteralPath $Output
Write-Output ('Wrote '+$Output)
