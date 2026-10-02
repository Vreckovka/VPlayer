# VPlayer behavioral tests

Use **Debug only**; see [build policy](build-policy.md). Prior Release runs do not constitute Debug validation.

The xUnit project tests production assemblies on their existing .NET Core 3.1 runtime.
It needs the sibling CustomLibraries checkout described in the root README, the .NET Core 3.1
desktop runtime, and a .NET SDK. Use x64 because CefSharp requires an explicit architecture.

Run from the repository root:

    dotnet test tests/VPlayer.Tests/VPlayer.Tests.csproj -c Debug -p:Platform=x64
    dotnet run --project tests/PlaylistOrder.Regression/PlaylistOrder.Regression.csproj -c Debug
    dotnet build VPlayer/VPlayer.csproj -c Debug -p:Platform=x64

The pinned xUnit adapter 2.4.1 supports .NET Core 3.1; newer adapter 2.5.3 requires .NET 6
and silently discovers zero tests when its fallback .NET Framework adapter is selected.

Tests use isolated temporary directories and SQLite contexts. They do not use the user's
media library, database, audio device, or live cloud services. Test results must show an
executed, nonzero test count; a successful build or a zero-test run is insufficient.

The repeatable worst-case performance runner and baseline/optimized comparison protocol are
documented in ../performance/README.md. Feature performance results are separate from unit tests.

Storage update regressions seed 10k long-name artists in an isolated SQLite
database. They gate a save to verify that awaiting an update includes persistence
and notification, check missing and unchanged records, and simulate a failed
write followed by a successful retry. All four failed before the fix.
UpdateEntityAsync now returns its worker task and actual save result; unchanged
writes no longer publish change events. The full suite executes 127 passing tests.