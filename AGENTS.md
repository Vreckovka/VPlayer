# Local production deployment

When the user requests deployment in this chat/project, publish a fresh production build: Release, x64, self-contained win-x64. Do not deploy a Debug build or reuse a stale published directory.

Increase `VPlayer/VersionAutoIncrement.cs` for every deployment, following the existing `VersionAutoIncrement.tt` formula and ensuring the new version exceeds both the current source version and installed version. Commit and push the version and completed changes to the authorized `codex/tests-and-performance` branch.

Publish into a new workspace-owned staging directory. Verify the version, compiler optimization flags, native VLC cache, and published file hashes before installation. Back up and preserve the user's library and settings. Never stop or automate the user's running VPlayer; require it to be closed before replacing installed files. Launch the installed app when requested.

Performance tests use Release x64 unless the user explicitly requests another configuration. Keep baseline and optimized configurations identical. Optimization remains paused until the user asks to resume it.