# VPlayer behavioral tests

The xUnit project tests production assemblies on their existing .NET Core 3.1 runtime.
It needs the sibling CustomLibraries checkout described in the root README, the .NET Core 3.1
desktop runtime, and a .NET SDK. Use x64 because CefSharp requires an explicit architecture.

Run from the repository root:

    dotnet test tests/VPlayer.Tests/VPlayer.Tests.csproj -p:Platform=x64
    dotnet run --project tests/PlaylistOrder.Regression/PlaylistOrder.Regression.csproj
    dotnet build VPlayer/VPlayer.csproj -p:Platform=x64

The pinned xUnit adapter 2.4.1 supports .NET Core 3.1; newer adapter 2.5.3 requires .NET 6
and silently discovers zero tests when its fallback .NET Framework adapter is selected.

Tests use isolated temporary directories and SQLite contexts. They do not use the user's
media library, database, audio device, or live cloud services. Test results must show an
executed, nonzero test count; a successful build or a zero-test run is insufficient.
