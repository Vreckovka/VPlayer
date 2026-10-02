param([Parameter(Mandatory=$true)][string]$NativeDirectory,[Parameter(Mandatory=$true)][string]$RunDirectory,[Parameter(Mandatory=$true)][string]$ProductionCommit)
$ErrorActionPreference='Stop'
$repo=Split-Path -Parent $PSScriptRoot
$destination=(Resolve-Path -LiteralPath $RunDirectory).Path
$native=(Resolve-Path -LiteralPath $NativeDirectory).Path
foreach($path in @($destination,$native)){if(-not $path.StartsWith($repo+'\',[StringComparison]::OrdinalIgnoreCase)){throw 'Cache checks require workspace-owned files.'}}
if(@(Get-ChildItem -LiteralPath $destination).Count){throw 'Use a new empty check directory.'}
$package=Join-Path $destination 'relocated-package'
New-Item -ItemType Directory -Path (Join-Path $package 'libvlc') -Force|Out-Null
$lib=Join-Path $package 'libvlc/win-x64'
Copy-Item -LiteralPath $native -Destination $lib -Recurse
$cache=Join-Path $lib 'plugins/plugins.dat'
if(-not (Test-Path -LiteralPath $cache)){throw 'Prepared cache required for relocation test.'}
$tool=Join-Path $repo 'tools/VPlayer.VlcCache/bin/Release/netcoreapp3.1/VPlayer.VlcCache.dll'
$benchmark=Join-Path $repo 'tests/VPlayer.Performance/bin/x64/Release/netcoreapp3.1/VPlayer.Performance.dll'
function Decode($name){
 $output=Join-Path $destination ($name+'.json')
 & dotnet $benchmark vlc-startup $lib $output $ProductionCommit *> (Join-Path $destination ($name+'.log'))
 if($LASTEXITCODE -ne 0){throw ($name+' decode failed')}
 return Get-Content -LiteralPath $output -Raw|ConvertFrom-Json
}
$relocated=Decode 'relocation'
Remove-Item -LiteralPath $cache
& dotnet msbuild (Join-Path $repo 'VPlayer/VPlayer.csproj') -t:PreparePublishedVlcPluginCache -p:Configuration=Release -p:Platform=x64 -p:PublishProfile=FolderProfile "-p:PublishDir=$package/" *> (Join-Path $destination 'publish-target.log')
if($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $cache)){throw 'Profile-based publish target failed to generate cache'}
[IO.File]::WriteAllBytes($cache,[byte[]](1,2,3,4))
& dotnet $tool $lib *> (Join-Path $destination 'regenerate.log')
if($LASTEXITCODE -ne 0 -or (Get-Item -LiteralPath $cache).Length -le 4){throw 'Corrupt cache was not replaced'}
$regenerated=Decode 'regenerated'
foreach($field in @('NativePayloadSha256','NativeVersion','NativeDlls','DecodedFrames','DurationMilliseconds','DecodeFailures')){if($relocated.$field -ne $regenerated.$field){throw ('Changed regenerated native result '+$field)}}
foreach($field in @('AudioFilters','VideoFilters')){if(($relocated.$field -join ',') -ne ($regenerated.$field -join ',')){throw ('Changed modules '+$field)}}
& dotnet $tool (Join-Path $destination 'does-not-exist') *> (Join-Path $destination 'incomplete-payload.log')
if($LASTEXITCODE -ne 1){throw 'Missing payload accepted'}
& dotnet $tool *> (Join-Path $destination 'invalid-arguments.log')
if($LASTEXITCODE -ne 1){throw 'Invalid arguments accepted'}
[IO.File]::WriteAllBytes($cache,[byte[]](1,2,3,4))
try{
 Set-ItemProperty -LiteralPath $cache -Name IsReadOnly -Value $true
 & dotnet $tool $lib *> (Join-Path $destination 'read-only-cache.log')
 if($LASTEXITCODE -ne 1){throw 'Read-only stale cache accepted'}
}finally{Set-ItemProperty -LiteralPath $cache -Name IsReadOnly -Value $false}
& dotnet $tool $lib *> (Join-Path $destination 'final-regenerate.log')
if($LASTEXITCODE -ne 0){throw 'Final regeneration failed'}
[ordered]@{Schema='vlc-cache-checks-v1';ProductionCommit=$ProductionCommit;NativeVersion=$relocated.NativeVersion;NativePayloadSha256=$relocated.NativePayloadSha256;RelocatedCacheDecodes=$true;PublishProfileSuppliesRuntimeAndGeneratesFreshCache=$true;CorruptCacheReplaced=$true;RegeneratedCacheDecodesSameFramesAndModules=$true;MissingPayloadRejected=$true;InvalidArgumentsRejected=$true;ReadOnlyStaleCacheRejected=$true;Passed=7} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $destination 'checks.json') -Encoding UTF8
Write-Output 'Seven native cache checks passed.'