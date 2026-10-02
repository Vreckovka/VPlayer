# Current optimization scope

Only these areas are in scope, in the order determined by measured bottlenecks:

| Area | Worst-case baseline requirement |
| --- | --- |
| Cold application startup | First launch after a restart, separately from warm disk-cache launches; separate disk I/O, assembly/JIT, database and first rendered frame. |
| Application data loading | Expanded copy of the current library; verify loaded counts and saved metadata. |
| Playlist loading | 100k entries including duplicates; verify order, metadata and rendered first/final rows. |
| Album and file information | Show existing stored data promptly; measure optional enrichment separately with slow/unavailable sources. |
| Playlist next/back | Rapid repeated skipping in a huge playlist; verify final track ownership and absence of stale callbacks or crashes. |
| Lyrics playback and auto-scroll | Long lyrics, seeks, rapid track changes and slow loads; verify the active line and scroll position. |
| Explorer files/folders loading | Large and deeply nested directories, inaccessible entries and interrupted navigation; verify results. |
| Explorer files/folders finding | Large directory trees, long queries, no matches and interrupted searches; verify matches. |

Freeze the first valid baseline before changing each area. Keep compact two-column baseline/optimized comparisons with signed percentages; preserve raw samples and failures. Optimize the largest measured delay within each area and across the areas. Previous statistics and other UI results remain historical evidence; further work on those features is out of scope.

Fresh process/profile measurements do not establish a cold disk-cache baseline. A restart-based cold measurement must be scheduled with the user; do not silently restart their PC or clear the system disk cache.

Local test release uses VPlayer/Properties/PublishProfiles/FolderProfile.pubxml (D:\VPlayer) and the existing VersionAutoIncrement.tt formula. Deployment replaces published application files after backing them up; it preserves the installed settings and roaming library database.

Keep the user's running VPlayer and installed files untouched. Benchmarks use an agent-owned build and copied data. Deploy only on a new explicit user request, after the user closes the app themselves.
