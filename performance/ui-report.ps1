param(
  [Parameter(Mandatory=$true)][string]$Baseline,
  [string]$Optimized,
  [string]$Output=(Join-Path $PSScriptRoot 'buffered-ui-results.md'),
  [string[]]$PhaseNames=@(
    'Application / initial playlist view ready',
    'Application / first window render',
    'UI / statistics / load and render'
  )
)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'report-format.ps1')
$fields=@('FixtureSha256','SoundItems','Playlists','WindowMode','Runtime','Configuration','ProcessorCount','OS')
function Read-Runs($path) {
  if(!$path){return @()}
  $runs=@(Get-Content -LiteralPath $path -Raw | ConvertFrom-Json)
  if(!$runs.Count){throw 'Empty UI series'}
  Assert-DiagnosticMode $runs @()
  foreach($run in $runs) {
    if($run.Commit -notmatch '^[0-9a-fA-F]{40}$'){throw 'UI runs require full commit hashes'}
    if($run.Commit -ne $runs[0].Commit){throw 'Mixed UI commits'}
    foreach($field in $fields) {
      if($null -eq $run.$field -or $run.$field -ne $runs[0].$field){throw "Missing or mixed UI field: $field"}
    }
  }
  return $runs
}
function Timing($runs,$name) {
  $complete=@($runs|Where-Object {$_.Status -eq 'Rendered' -and @($_.Phases|Where-Object Name -eq $name).Count -eq 1})
  if(!$complete.Count){throw "No completed samples: $name"}
  $values=@($complete|ForEach-Object {($_.Phases|Where-Object Name -eq $name).Milliseconds}|Sort-Object)
  $middle=[int][Math]::Floor($values.Count/2)
  $median=if($values.Count%2){$values[$middle]}else{($values[$middle-1]+$values[$middle])/2}
  return [pscustomobject]@{Value=$median;Partial=($complete.Count -ne $runs.Count)}
}
$a=Read-Runs $Baseline
$b=Read-Runs $Optimized
if($b.Count) {
  Assert-DiagnosticMode $a $b
  foreach($field in $fields) {
    if($a[0].$field -ne $b[0].$field){throw "Incompatible UI runs: $field"}
  }
}
$rows=foreach($name in $PhaseNames) {
  $old=Timing $a $name
  $new=if($b.Count){Timing $b $name}else{$null}
  [pscustomobject]@{Name=($name -replace '^(Application|UI) / ','');Old=$old;New=$new}
}
$lines=[Collections.Generic.List[string]]::new()
$lines.Add('# Buffered UI baseline')
$lines.Add('')
$lines.Add('207k fully played sound items; 5,310 playlists. Buffered diagnostics; median times. - % = less time; + % = more time.')
$lines.Add('')
$lines.Add('| Baseline now | New optimized version |')
$lines.Add('| --- | --- |')
foreach($row in @($rows|Sort-Object {$_.Old.Value} -Descending)) {
  $left=(Format-Time $row.Old.Value)+$(if($row.Old.Partial){'*'}else{''})
  $right=if($row.New){(Format-Time $row.New.Value)+(Format-Change $row.Old.Value $row.New.Value)+$(if($row.New.Partial){'*'}else{''})}else{'Pending'}
  $lines.Add('| **'+$row.Name+'** — '+$left+' | '+$right+' |')
}
$lines.Add('')
$lines.Add('New measurement protocol: no speedup is credited against older synchronous traces. Full samples and provenance remain in JSON.')
if(@($rows|Where-Object {$_.Old.Partial -or $_.New.Partial}).Count){$lines.Add('* Timing uses completed samples; incomplete launches remain in JSON.')}
$lines | Set-Content -LiteralPath $Output
Write-Output ('Wrote '+$Output)
