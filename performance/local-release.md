# Local test release

**7.6.9771.34514** installed in `D:\VPlayer`, increased from `7.6.9771.31678`. Source: `3a69c4e8` on `codex/tests-and-performance`.

Includes the latest [playlist collection optimization and subscription fixes](iterations/music-collection-tracking-results-b65ef9ce.md), earlier playlist loading and rapid-skip fixes, and the [startup window-order fix](window-startup-results.md).

Release publish succeeded. All application files verified; library and settings preserved during deployment. The same production changes passed 231 regression tests; this release changes only the version using the existing updater formula.

Installed startup, saved metadata, a 100k-item ordered playlist, its last visible track and both search cases passed the smoke check. Initial, first-track and last-track screenshots were reviewed. This single check is not a performance comparison or an audio playback completion test.

VPlayer reopened during the first replacement attempt. The application files were rolled back and verified; the retry succeeded after it stayed closed. Backups remain in `artifacts/local-deployment`. A normal VPlayer session is now running; its live library changes are preserved.

[Release verification](iterations/local-release-3a69c4e8.json). Remaining optimization scope: [focus.md](focus.md).