# Local production release

**7.6.9771.41550** is installed in `D:\VPlayer` and running, upgraded from **7.6.9771.34514**. Fresh **Release x64**, self-contained `win-x64` publish from `3eaa0a8a` on `codex/tests-and-performance`; compiler optimization flags and the newly generated VLC cache were verified.

Includes the branch's VLC startup, incoming playlist construction, lyrics animation/cached lyrics, and file-browser loading changes. **258 Release tests passed** on the same production implementations; three existing file-browser search regressions remain excluded and unfixed.

All **1,306** published application files verified; **36** changed. All **1,033** library/settings files remained unchanged during installation. Read locks were released before launch. The installed main window is responding. This is deployment verification, not a playback or performance comparison.

The disk initially had only 2.6 MB free. Four obsolete published build directories were removed with authorization, freeing 3.4 GiB. Backups remain under `artifacts/local-deployment`, including the successful rollback snapshot. The prior failed attempts rolled back application files; live user data was preserved.

Every requested deployment here must use a fresh production publish and an increased version. Optimization remains paused. [Verification](iterations/local-release-3eaa0a8a.json).