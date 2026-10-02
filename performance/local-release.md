# Local test release

**7.6.9771.31678** installed in `D:\VPlayer`, increased from `7.6.9771.25313`. Source: `a4c5ef2d` on `codex/tests-and-performance`.

Includes the [playlist loading optimizations](music-playlist-ui-results.md), rapid-skip fixes and [startup window-order fix](window-startup-results.md).

Release publish succeeded. All 1,310 application files verified; 1,031 protected library/settings files were unchanged during deployment. Backups and the full deployment record remain in `artifacts/local-deployment`.

The existing 223-test suite and successful 100k-item branch smoke cover the same production source; this release changes only its version. Installed startup and the initial library view rendered and were visually checked. The installed 100k-item smoke exited during playlist reading and remains incomplete; it is not a performance result.

The first deployment detected a changing live database and rolled the application files back, with rollback hashes verified. The retry backed up and preserved the updated library. A separate normal installed VPlayer session is now running; subsequent library changes were left intact.

[Release verification](iterations/local-release-a4c5ef2d.json). Other optimization categories in [focus.md](focus.md) remain pending.