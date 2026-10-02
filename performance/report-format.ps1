# Shared display helpers. Raw JSON retains precision, samples and allocation data.
function Format-Time($milliseconds) {
  if($null -eq $milliseconds -or $milliseconds -lt 0) {return 'n/a'}
  if($milliseconds -ge 1000) {
    return ($milliseconds/1000).ToString('0.#',[Globalization.CultureInfo]::InvariantCulture)+'k ms'
  }
  $pattern=if($milliseconds -ge 10){'0.#'}else{'0.##'}
  return ([double]$milliseconds).ToString($pattern,[Globalization.CultureInfo]::InvariantCulture)+' ms'
}
function Format-Change($baseline,$current) {
  if($null -eq $baseline -or $null -eq $current -or $baseline -le 0 -or $current -lt 0) {return ''}
  $change=100*($current-$baseline)/$baseline
  if([Math]::Abs($change) -lt 0.05) {return ' (0%)'}
  $sign=if($change -gt 0){'+'}else{'-'}
  return ' ('+$sign+([Math]::Abs($change)).ToString('0.#',[Globalization.CultureInfo]::InvariantCulture)+'%)'
}

# Old traces wrote synchronously; never attribute a measurement-protocol change
# to application optimization. Missing mode is the original protocol.
function Assert-DiagnosticMode($reference,$runs) {
  $modes=@(@($reference)+@($runs) | Where-Object {$null -ne $_} | ForEach-Object {
    if($_.DiagnosticMode){$_.DiagnosticMode}else{'synchronous-v1'}
  } | Select-Object -Unique)
  if($modes.Count -gt 1){throw 'Incompatible diagnostic modes; establish a new UI baseline.'}
}