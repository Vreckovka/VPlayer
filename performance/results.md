# Worst-case performance comparison

Baseline commit: 36bd8be4ea300705452da308032c31ee45b81ed2. Optimized commit: ffea769d473b811865fe36d3e023f34a8b3b6d2b.
Workload: 207110 sound items, 310 playlists, plus 1,000 / 10,000 / 100,000-entry stress playlists. Release x64 on .NET 3.1.32.
Fixture SHA-256: 89737F48D5184B0C3517FCD06872366D11918C621B7005589EEEADE6DC4611AE. Baseline samples are immutable.

## Application startup

Startup comparison commit: ffea769d473b811865fe36d3e023f34a8b3b6d2b.
The initial populated-view milestone has a separate baseline at f9bdaa8a4b6c5e1612965d5759179e160b6833b7 after repairing the startup crash. Earlier failed launches cannot provide this timing.

| Baseline now | New optimized version |
| --- | --- |
| **Application / initial playlist view ready** — 5 774,52 ms median; 6 897,59 ms max; reached in 7/7 launches | 4 850,62 ms median; 9 918,89 ms max; reached in 7/7 launches |
| **Application / first window render** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 3 195,20 ms median; 7 399,94 ms max; reached in 7/7 launches |
| **Application / initialization** — 1 915,51 ms median; 2 211,14 ms max; reached in 7/7 launches | 1 698,91 ms median; 2 796,41 ms max; reached in 7/7 launches |
| **Application / shell construction** — 927,84 ms median; 1 124,91 ms max; reached in 7/7 launches | 881,32 ms median; 975,26 ms max; reached in 7/7 launches |
| **Application / module registration** — 871,23 ms median; 1 226,52 ms max; reached in 7/7 launches | 615,68 ms median; 1 647,68 ms max; reached in 7/7 launches |
| **Application / WindowsPlayerNinjectModule** — 795,11 ms median; 1 179,31 ms max; reached in 7/7 launches | 552,93 ms median; 1 271,94 ms max; reached in 7/7 launches |
| **Application / show shell** — 722,53 ms median; 1 012,92 ms max; reached in 7/7 launches | 837,03 ms median; 1 008,10 ms max; reached in 7/7 launches |
| **Application / video player activation** — 568,91 ms median; 674,25 ms max; reached in 7/7 launches | 405,06 ms median; 450,22 ms max; reached in 7/7 launches |
| **Application / music player activation** — 94,80 ms median; 479,84 ms max; reached in 7/7 launches | 82,02 ms median; 626,02 ms max; reached in 7/7 launches |
| **Application / library module** — 58,91 ms median; 85,93 ms max; reached in 7/7 launches | 52,60 ms median; 55,41 ms max; reached in 7/7 launches |
| **Application / container activation** — 48,01 ms median; 73,33 ms max; reached in 7/7 launches | 81,87 ms median; 597,99 ms max; reached in 7/7 launches |
| **Application / settings** — 22,48 ms median; 29,24 ms max; reached in 7/7 launches | 52,04 ms median; 579,72 ms max; reached in 7/7 launches |
| **Application / OpenCV native initialization** — 21,41 ms median; 29,61 ms max; reached in 7/7 launches | 22,09 ms median; 24,36 ms max; reached in 7/7 launches |
| **Application / UPnPNinjectModule** — 8,90 ms median; 11,76 ms max; reached in 7/7 launches | 7,64 ms median; 9,91 ms max; reached in 7/7 launches |
| **Application / IPTVModule** — 7,75 ms median; 11,04 ms max; reached in 7/7 launches | 7,58 ms median; 10,25 ms max; reached in 7/7 launches |
| **Application / VPlayerCoreModule** — 5,84 ms median; 8,41 ms max; reached in 7/7 launches | 7,89 ms median; 191,84 ms max; reached in 7/7 launches |

## Data

| Baseline now | New optimized version |
| --- | --- |
| **Data / sound items** — 2 640,81 ms median; 3 793,30 ms p95; 8 263,00 ms first; 368,7 MiB allocated | 2 733,88 ms median; 3 081,25 ms p95; 3 577,66 ms first; 368,7 MiB allocated; 3,5% slower |
| **Data / albums** — 13,08 ms median; 19,82 ms p95; 58,70 ms first; 1,4 MiB allocated | 13,36 ms median; 16,80 ms p95; 72,02 ms first; 1,4 MiB allocated; 2,2% slower |
| **Data / artists** — 7,83 ms median; 10,16 ms p95; 18,24 ms first; 0,1 MiB allocated | 11,48 ms median; 22,67 ms p95; 42,19 ms first; 0,1 MiB allocated; 46,6% slower |

## Playlist

| Baseline now | New optimized version |
| --- | --- |
| **Playlist / 100000 entries** — 22 145,50 ms median; 27 065,15 ms p95; 25 917,40 ms first; 811,7 MiB allocated | 2 751,62 ms median; 2 824,68 ms p95; 2 811,64 ms first; 806,4 MiB allocated; 87,6% faster |
| **Playlist / 10000 entries** — 1 009,48 ms median; 2 145,33 ms p95; 1 028,93 ms first; 82,1 MiB allocated | 285,72 ms median; 357,03 ms p95; 360,01 ms first; 81,6 MiB allocated; 71,7% faster |
| **Playlist / 1000 entries** — 515,15 ms median; 606,56 ms p95; 5 029,82 ms first; 8,5 MiB allocated | 85,86 ms median; 145,08 ms p95; 1 309,30 ms first; 8,5 MiB allocated; 83,3% faster |
| **Playlist / summaries** — 9,54 ms median; 10,14 ms p95; 75,27 ms first; 0,3 MiB allocated | 10,06 ms median; 11,46 ms p95; 18,56 ms first; 0,3 MiB allocated; 5,5% slower |

## UI

| Baseline now | New optimized version |
| --- | --- |
| **UI / title sort** — 1 677,83 ms median; 2 473,30 ms p95; 1 894,95 ms first; 5,5 MiB allocated | 1 176,41 ms median; 1 237,18 ms p95; 1 204,41 ms first; 5,5 MiB allocated; 29,9% faster |
| **UI / playlist view models** — 153,31 ms median; 342,36 ms p95; 142,62 ms first; 39,5 MiB allocated | 114,31 ms median; 316,07 ms p95; 151,39 ms first; 39,5 MiB allocated; 25,4% faster |
| **UI / title search** — 47,60 ms median; 49,76 ms p95; 78,93 ms first; 0,1 MiB allocated | 38,49 ms median; 40,60 ms p95; 208,20 ms first; 0,1 MiB allocated; 19,1% faster |
| **UI / virtualized list scroll** — 8,28 ms median; 10,46 ms p95; 11,19 ms first; 0,0 MiB allocated | 7,82 ms median; 8,82 ms p95; 9,65 ms first; 0,0 MiB allocated; 5,5% faster |
| **UI / virtualized list layout** — 6,36 ms median; 330,16 ms p95; 394,69 ms first; 0,0 MiB allocated | 5,57 ms median; 246,58 ms p95; 88,22 ms first; 0,0 MiB allocated; 12,5% faster |

## Lyrics

| Baseline now | New optimized version |
| --- | --- |
| **Lyrics / 100000 lines** — 3,74 ms median; 4,27 ms p95; 18,90 ms first; 0,0 MiB allocated | 4,25 ms median; 11,97 ms p95; 23,17 ms first; 0,0 MiB allocated; 13,7% slower |
| **Lyrics / 10000 lines** — 2,84 ms median; 3,56 ms p95; 288,78 ms first; 0,0 MiB allocated | 1,93 ms median; 2,15 ms p95; 3,92 ms first; 0,0 MiB allocated; 32,2% faster |
| **Lyrics / 1000 lines** — 1,77 ms median; 2,02 ms p95; 14,95 ms first; 0,0 MiB allocated | 1,51 ms median; 2,01 ms p95; 9,88 ms first; 0,0 MiB allocated; 14,8% faster |

## Spectrum

| Baseline now | New optimized version |
| --- | --- |
| **Spectrum / changing frames** — 11,78 ms median; 14,64 ms p95; 15,95 ms first; 0,2 MiB allocated | 4,39 ms median; 5,36 ms p95; 5,26 ms first; 0,2 MiB allocated; 62,7% faster |
| **Spectrum / stable frames** — 5,99 ms median; 18,51 ms p95; 236,76 ms first; 0,2 MiB allocated | 1,32 ms median; 1,98 ms p95; 58,03 ms first; 0,2 MiB allocated; 78,0% faster |

## Coverage still requiring end-to-end scenarios

| Baseline now | New optimized version |
| --- | --- |
| **Library card templates and scrolling** — Not measured yet | Pending |
| **Navigation and detail views** — Not measured yet | Pending |
| **File-browser folders and thumbnails** — Not measured yet | Pending |
| **Settings and modal dialogs** — Not measured yet | Pending |
| **Video and fullscreen transitions** — Not measured yet | Pending |
| **Cloud and network timeout handling** — Not measured yet | Pending |
| **LibraryCollection full load/filter publication** — Not measured yet | Pending |

This comparison changes playlist loading and repairs startup. Other feature metrics are controls: timing differences in unchanged code are observations and must not be credited to the playlist optimization.

Each feature metric is one operation except lyrics (10,000 seeks) and spectrum (1,000 frames). Divide batched results by their operation count before ranking across categories. UI list scenarios use an offscreen text-row ListBox, not application card templates. First samples share one process, so only the first metric includes process-cold EF initialization. Startup uses fresh processes with warm OS caches and fresh default settings; it does not include every background service becoming ready. Startup phase scopes overlap. Allocation counts cover managed allocations across threads and exclude native bitmap/database memory.

See README.md for workload boundaries and the optimization protocol.
