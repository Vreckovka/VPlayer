param(
  [ValidateSet('baseline','optimized')][string]$Mode='baseline',
  [string]$FixtureDirectory='artifacts/performance/library-worst-case',
  [int]$StartupRuns=7
)
$ErrorActionPreference='Stop'
$repository=Split-Path -Parent $PSScriptRoot
Push-Location $repository
try {
  $fixture=(Resolve-Path -LiteralPath $FixtureDirectory).Path
  $commit=(& git rev-parse HEAD).Trim()
  $runDirectory=Join-Path $repository ('artifacts/performance/runs/'+$Mode+'-'+$commit.Substring(0,8)+'-'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
  New-Item -ItemType Directory -Path $runDirectory | Out-Null
  & dotnet build tests/VPlayer.Performance/VPlayer.Performance.csproj -c Release -p:Platform=x64 -v quiet *> (Join-Path $runDirectory 'build-runner.log')
  if($LASTEXITCODE -ne 0) {throw 'Performance runner build failed.'}
  & dotnet build VPlayer/VPlayer.csproj -c Release -p:Platform=x64 -v quiet *> (Join-Path $runDirectory 'build-app.log')
  if($LASTEXITCODE -ne 0) {throw 'Application build failed.'}
  # Run no builds/tests concurrently with measurement.
  & dotnet tests/VPlayer.Performance/bin/x64/Release/netcoreapp3.1/VPlayer.Performance.dll run $fixture (Join-Path $runDirectory 'features.json') $commit
  if($LASTEXITCODE -ne 0) {throw 'Feature benchmarks failed.'}
  $application=(Resolve-Path 'VPlayer/bin/x64/Release/netcoreapp3.1/VPlayer.exe').Path
  $originalValues=@{}
  foreach($name in @('VPLAYER_BENCHMARK_DIRECTORY','VPLAYER_PERFORMANCE_RUN_FILE','VPLAYER_PERFORMANCE_COMMIT','VPLAYER_PERFORMANCE_EXIT_AFTER_RENDER')) {
    $originalValues[$name]=[Environment]::GetEnvironmentVariable($name,'Process')
  }
  try {
    for($i=0;$i -lt $StartupRuns;$i++) {
      # Each launch gets a disposable DB copy; startup/background work cannot modify the baseline fixture.
      $profile=Join-Path $runDirectory ('startup-profile-'+$i)
      New-Item -ItemType Directory -Path $profile | Out-Null
      Copy-Item -LiteralPath (Join-Path $fixture 'VPlayerDatabase.db') -Destination (Join-Path $profile 'VPlayerDatabase.db')
      $env:VPLAYER_BENCHMARK_DIRECTORY=$profile
      $env:VPLAYER_PERFORMANCE_RUN_FILE=Join-Path $runDirectory ('startup-'+$i+'.json')
      $env:VPLAYER_PERFORMANCE_COMMIT=$commit
      $env:VPLAYER_PERFORMANCE_EXIT_AFTER_RENDER='1'
      $launch=@{
        FilePath=$application;WorkingDirectory=(Split-Path -Parent $application);WindowStyle='Hidden';PassThru=$true
        RedirectStandardOutput=(Join-Path $runDirectory ('startup-'+$i+'.stdout.log'))
        RedirectStandardError=(Join-Path $runDirectory ('startup-'+$i+'.stderr.log'))
      }
      $process=Start-Process @launch
      if(!$process.WaitForExit(60000)) {
        Stop-Process -Id $process.Id -Force
                $timedOut=@{Status='Timeout';Commit=$commit;Phases=@();TimeoutSeconds=60}
        if(Test-Path -LiteralPath $env:VPLAYER_PERFORMANCE_RUN_FILE) {
          $timedOut=Get-Content -LiteralPath $env:VPLAYER_PERFORMANCE_RUN_FILE -Raw | ConvertFrom-Json
          $timedOut.Status='Timeout'
          $timedOut | Add-Member -NotePropertyName TimeoutSeconds -NotePropertyValue 60 -Force
        }
        $timedOut | ConvertTo-Json -Depth 20 | Set-Content $env:VPLAYER_PERFORMANCE_RUN_FILE
      } elseif(!(Test-Path -LiteralPath $env:VPLAYER_PERFORMANCE_RUN_FILE)) {
        @{Status='ExitedWithoutRender';ExitCode=$process.ExitCode;Commit=$commit;Phases=@()} | ConvertTo-Json | Set-Content $env:VPLAYER_PERFORMANCE_RUN_FILE
      }
    }
  } finally {
    foreach($name in $originalValues.Keys) {[Environment]::SetEnvironmentVariable($name,$originalValues[$name],'Process')}
  }
  Write-Output ('Saved '+$runDirectory)
} finally {Pop-Location}
