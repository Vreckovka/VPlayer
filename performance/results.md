# Worst-case performance comparison

Baseline commit: 36bd8be4ea300705452da308032c31ee45b81ed2. Optimized commit: pending.
Workload: 207110 sound items, 310 playlists, plus 1,000 / 10,000 / 100,000-entry stress playlists. Release x64 on .NET 3.1.32.
Fixture SHA-256: 89737F48D5184B0C3517FCD06872366D11918C621B7005589EEEADE6DC4611AE. Baseline samples are immutable.

## Application startup

| Baseline now | New optimized version |
| --- | --- |
| **Application / first window render** — Failed in 7/7 launches (Failed: System.NullReferenceException); no valid timing | Pending |
| **Application / initialization** — 1 915,51 ms median; 2 211,14 ms max; 7 launches | Pending |
| **Application / shell construction** — 927,84 ms median; 1 124,91 ms max; 7 launches | Pending |
| **Application / module registration** — 871,23 ms median; 1 226,52 ms max; 7 launches | Pending |
| **Application / container activation** — 48,01 ms median; 73,33 ms max; 7 launches | Pending |
| **Application / settings** — 22,48 ms median; 29,24 ms max; 7 launches | Pending |
| **Application / VPlayerCoreModule** — 5,84 ms median; 8,41 ms max; 7 launches | Pending |
| **Application / IPTVModule** — 7,75 ms median; 11,04 ms max; 7 launches | Pending |
| **Application / UPnPNinjectModule** — 8,90 ms median; 11,76 ms max; 7 launches | Pending |
| **Application / library module** — 58,91 ms median; 85,93 ms max; 7 launches | Pending |
| **Application / video player activation** — 568,91 ms median; 674,25 ms max; 7 launches | Pending |
| **Application / music player activation** — 94,80 ms median; 479,84 ms max; 7 launches | Pending |
| **Application / WindowsPlayerNinjectModule** — 795,11 ms median; 1 179,31 ms max; 7 launches | Pending |
| **Application / OpenCV native initialization** — 21,41 ms median; 29,61 ms max; 7 launches | Pending |
| **Application / show shell** — 722,53 ms median; 1 012,92 ms max; 7 launches | Pending |

## Data

| Baseline now | New optimized version |
| --- | --- |
| **Data / sound items** — 2 640,81 ms median; 3 793,30 ms p95; 8 263,00 ms first; 368,7 MiB allocated | Pending |
| **Data / albums** — 13,08 ms median; 19,82 ms p95; 58,70 ms first; 1,4 MiB allocated | Pending |
| **Data / artists** — 7,83 ms median; 10,16 ms p95; 18,24 ms first; 0,1 MiB allocated | Pending |

## Playlist

| Baseline now | New optimized version |
| --- | --- |
| **Playlist / 100000 entries** — 22 145,50 ms median; 27 065,15 ms p95; 25 917,40 ms first; 811,7 MiB allocated | Pending |
| **Playlist / 10000 entries** — 1 009,48 ms median; 2 145,33 ms p95; 1 028,93 ms first; 82,1 MiB allocated | Pending |
| **Playlist / 1000 entries** — 515,15 ms median; 606,56 ms p95; 5 029,82 ms first; 8,5 MiB allocated | Pending |
| **Playlist / summaries** — 9,54 ms median; 10,14 ms p95; 75,27 ms first; 0,3 MiB allocated | Pending |

## UI

| Baseline now | New optimized version |
| --- | --- |
| **UI / title sort** — 1 677,83 ms median; 2 473,30 ms p95; 1 894,95 ms first; 5,5 MiB allocated | Pending |
| **UI / playlist view models** — 153,31 ms median; 342,36 ms p95; 142,62 ms first; 39,5 MiB allocated | Pending |
| **UI / title search** — 47,60 ms median; 49,76 ms p95; 78,93 ms first; 0,1 MiB allocated | Pending |
| **UI / virtualized list scroll** — 8,28 ms median; 10,46 ms p95; 11,19 ms first; 0,0 MiB allocated | Pending |
| **UI / virtualized list layout** — 6,36 ms median; 330,16 ms p95; 394,69 ms first; 0,0 MiB allocated | Pending |

## Lyrics

| Baseline now | New optimized version |
| --- | --- |
| **Lyrics / 100000 lines** — 3,74 ms median; 4,27 ms p95; 18,90 ms first; 0,0 MiB allocated | Pending |
| **Lyrics / 10000 lines** — 2,84 ms median; 3,56 ms p95; 288,78 ms first; 0,0 MiB allocated | Pending |
| **Lyrics / 1000 lines** — 1,77 ms median; 2,02 ms p95; 14,95 ms first; 0,0 MiB allocated | Pending |

## Spectrum

| Baseline now | New optimized version |
| --- | --- |
| **Spectrum / changing frames** — 11,78 ms median; 14,64 ms p95; 15,95 ms first; 0,2 MiB allocated | Pending |
| **Spectrum / stable frames** — 5,99 ms median; 18,51 ms p95; 236,76 ms first; 0,2 MiB allocated | Pending |

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

Each feature metric is one operation except lyrics (10,000 seeks) and spectrum (1,000 frames). Divide batched results by their operation count before ranking across categories. UI list scenarios use an offscreen text-row ListBox, not application card templates. First samples share one process, so only the first metric includes process-cold EF initialization. Startup uses fresh processes with warm OS caches and fresh default settings; it does not include every background service becoming ready. Startup phase scopes overlap. Allocation counts cover managed allocations across threads and exclude native bitmap/database memory.

See README.md for workload boundaries and the optimization protocol.
