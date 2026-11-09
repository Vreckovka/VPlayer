param(
    [Parameter(Mandatory)][string] $ClangPath,
    [string] $VlcSdkPath = (Join-Path $env:USERPROFILE '.nuget\packages\videolan.libvlc.windows\3.0.20\build\x64'),
    [string] $OutputDirectory = (Join-Path $PSScriptRoot '..\..\artifacts\native-subtitles')
)

$ErrorActionPreference = 'Stop'
$clang = (Resolve-Path -LiteralPath $ClangPath).Path
$sdk = (Resolve-Path -LiteralPath $VlcSdkPath).Path
$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$parser = Join-Path $PSScriptRoot 'ass_text.c'
$tests = Join-Path $output 'ass-text-tests.exe'

& $clang -std=c11 -O2 -Wall -Wextra -Werror $parser (Join-Path $PSScriptRoot 'test_ass_text.c') -o $tests
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $tests
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$plugin = Join-Path $output 'libvplayer_ass_text_plugin.dll'
& $clang -std=gnu11 -O2 -Wall -Wextra -Wno-unused-parameter -shared -s '-Wl,--no-insert-timestamp' `
    -D__PLUGIN__ -DMODULE_NAME=vplayer_ass_text '-DMODULE_STRING="vplayer_ass_text"' `
    -isystem (Join-Path $sdk 'include\vlc\plugins') (Join-Path $PSScriptRoot 'vlc_decoder.c') `
    $parser (Join-Path $sdk 'libvlccore.lib') -o $plugin
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Get-FileHash -LiteralPath $plugin -Algorithm SHA256
