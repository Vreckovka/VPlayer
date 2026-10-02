# Worst-case performance comparison

Baseline commit: 36bd8be4ea300705452da308032c31ee45b81ed2. Optimized commit: cc489285a4459a2b7082f73deab578a3005664e7.
Workload: 207110 sound items, 310 playlists, plus 1,000 / 10,000 / 100,000-entry stress playlists. Release x64 on .NET 3.1.32.
Fixture SHA-256: 89737F48D5184B0C3517FCD06872366D11918C621B7005589EEEADE6DC4611AE. Baseline samples are immutable.

## Application startup

Startup comparison commit: cc489285a4459a2b7082f73deab578a3005664e7.
The initial populated-view milestone has a separate baseline at f9bdaa8a4b6c5e1612965d5759179e160b6833b7 after repairing the startup crash. Earlier failed launches cannot provide this timing.
Additional startup phases use the first recorded baseline containing that phase. Baseline commits: ed0d248e3d8e20322d6672c3262c08ded293a0b1, 94ed1e6123d8a25aef29bf8dd0a4b1e9e3adccb9. These were recorded before query deferral.

| Baseline now | New optimized version |
| --- | --- |
| **Application / initial playlist view ready** — 5 774,52 ms median; 6 897,59 ms max; reached in 7/7 launches | 5 108,83 ms median; 9 353,22 ms max; reached in 7/7 launches |
| **Application / initial playlist view ready before query deferral** — 5 108,71 ms median; 10 111,84 ms max; reached in 7/7 launches | 5 108,83 ms median; 9 353,22 ms max; reached in 7/7 launches |
| **Application / first window render** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 3 302,87 ms median; 5 625,18 ms max; reached in 7/7 launches |
| **Application / first window render before query deferral** — 3 528,59 ms median; 7 749,60 ms max; reached in 7/7 launches | 3 302,87 ms median; 5 625,18 ms max; reached in 7/7 launches |
| **Application / first window render after crash fix** — 3 503,49 ms median; 5 118,84 ms max; reached in 7/7 launches | 3 302,87 ms median; 5 625,18 ms max; reached in 7/7 launches |
| **Application / initialization** — 1 915,51 ms median; 2 211,14 ms max; reached in 7/7 launches | 1 924,16 ms median; 2 442,09 ms max; reached in 7/7 launches |
| **Application / main window view model initialization** — 998,39 ms median; 1 266,93 ms max; reached in 7/7 launches | 538,28 ms median; 1 303,80 ms max; reached in 7/7 launches |
| **Application / shell construction** — 927,84 ms median; 1 124,91 ms max; reached in 7/7 launches | 656,97 ms median; 1 411,61 ms max; reached in 7/7 launches |
| **Application / module registration** — 871,23 ms median; 1 226,52 ms max; reached in 7/7 launches | 672,47 ms median; 1 339,94 ms max; reached in 7/7 launches |
| **Application / WindowsPlayerNinjectModule** — 795,11 ms median; 1 179,31 ms max; reached in 7/7 launches | 569,64 ms median; 853,50 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / UI dispatch and publication** — 760,78 ms median; 1 161,19 ms max; reached in 7/7 launches | 179,16 ms median; 1 090,09 ms max; reached in 7/7 launches |
| **Application / navigation view model construction** — 746,87 ms median; 875,11 ms max; reached in 7/7 launches | 203,49 ms median; 324,77 ms max; reached in 7/7 launches |
| **Application / show shell** — 722,53 ms median; 1 012,92 ms max; reached in 7/7 launches | 808,99 ms median; 1 033,09 ms max; reached in 7/7 launches |
| **Application / video player activation** — 568,91 ms median; 674,25 ms max; reached in 7/7 launches | 412,93 ms median; 475,75 ms max; reached in 7/7 launches |
| **Application / library SoundItemPlaylistsViewModel / navigation construction** — 534,46 ms median; 634,59 ms max; reached in 7/7 launches | 11,70 ms median; 14,64 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / post-load callback** — 213,90 ms median; 293,86 ms max; reached in 7/7 launches | 199,47 ms median; 676,86 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / playlist preparation** — 186,74 ms median; 277,25 ms max; reached in 7/7 launches | 173,75 ms median; 656,54 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / query** — 178,60 ms median; 1 253,93 ms max; reached in 7/7 launches | 726,00 ms median; 1 040,52 ms max; reached in 7/7 launches |
| **Application / player controls activation** — 109,83 ms median; 152,29 ms max; reached in 7/7 launches | 142,21 ms median; 927,46 ms max; reached in 7/7 launches |
| **Application / music player activation** — 94,80 ms median; 479,84 ms max; reached in 7/7 launches | 85,66 ms median; 207,82 ms max; reached in 7/7 launches |
| **Application / navigation activation** — 84,82 ms median; 125,73 ms max; reached in 7/7 launches | 97,95 ms median; 111,95 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / current track metadata** — 77,64 ms median; 118,04 ms max; reached in 7/7 launches | 60,64 ms median; 77,86 ms max; reached in 7/7 launches |
| **Application / benchmark first-frame screenshot** — 76,44 ms median; 175,27 ms max; reached in 7/7 launches | 67,16 ms median; 81,10 ms max; reached in 7/7 launches |
| **Application / main window XAML** — 66,49 ms median; 106,70 ms max; reached in 7/7 launches | 77,97 ms median; 156,56 ms max; reached in 7/7 launches |
| **Application / library module** — 58,91 ms median; 85,93 ms max; reached in 7/7 launches | 48,79 ms median; 73,39 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / view model construction** — 53,53 ms median; 116,66 ms max; reached in 7/7 launches | 49,85 ms median; 53,19 ms max; reached in 7/7 launches |
| **Application / container activation** — 48,01 ms median; 73,33 ms max; reached in 7/7 launches | 90,48 ms median; 467,91 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / pinned items** — 35,43 ms median; 45,44 ms max; reached in 7/7 launches | 31,20 ms median; 182,47 ms max; reached in 7/7 launches |
| **Application / library IArtistsViewModel / navigation construction** — 35,30 ms median; 45,64 ms max; reached in 7/7 launches | 10,87 ms median; 16,80 ms max; reached in 7/7 launches |
| **Application / settings** — 22,48 ms median; 29,24 ms max; reached in 7/7 launches | 47,29 ms median; 441,20 ms max; reached in 7/7 launches |
| **Application / OpenCV native initialization** — 21,41 ms median; 29,61 ms max; reached in 7/7 launches | 21,72 ms median; 27,78 ms max; reached in 7/7 launches |
| **Application / audio device enumeration** — 21,27 ms median; 40,40 ms max; reached in 7/7 launches | 22,81 ms median; 29,32 ms max; reached in 7/7 launches |
| **Application / player controls construction** — 18,53 ms median; 24,78 ms max; reached in 7/7 launches | 20,49 ms median; 28,61 ms max; reached in 7/7 launches |
| **Application / library WindowsFileBrowserViewModel / navigation construction** — 17,10 ms median; 23,44 ms max; reached in 7/7 launches | 14,53 ms median; 37,68 ms max; reached in 7/7 launches |
| **Application / library SettingsViewModel / navigation construction** — 15,70 ms median; 19,55 ms max; reached in 7/7 launches | 13,28 ms median; 43,41 ms max; reached in 7/7 launches |
| **Application / library IAlbumsViewModel / navigation construction** — 13,50 ms median; 16,86 ms max; reached in 7/7 launches | 11,44 ms median; 14,62 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / UI publication** — 12,89 ms median; 19,36 ms max; reached in 7/7 launches | 9,99 ms median; 16,53 ms max; reached in 7/7 launches |
| **Application / library PCloudManagerViewModel / navigation construction** — 12,89 ms median; 15,42 ms max; reached in 7/7 launches | 12,57 ms median; 13,92 ms max; reached in 7/7 launches |
| **Application / library VideoPlaylistsViewModel / navigation construction** — 12,34 ms median; 14,67 ms max; reached in 7/7 launches | 11,87 ms median; 12,73 ms max; reached in 7/7 launches |
| **Application / library TvShowsViewModel / navigation construction** — 11,64 ms median; 14,30 ms max; reached in 7/7 launches | 10,96 ms median; 14,38 ms max; reached in 7/7 launches |
| **Application / library StatisticsViewModel / navigation construction** — 11,16 ms median; 19,42 ms max; reached in 7/7 launches | 10,87 ms median; 11,64 ms max; reached in 7/7 launches |
| **Application / library UPnPManagerViewModel / navigation construction** — 9,22 ms median; 11,01 ms max; reached in 7/7 launches | 8,96 ms median; 11,46 ms max; reached in 7/7 launches |
| **Application / UPnPNinjectModule** — 8,90 ms median; 11,76 ms max; reached in 7/7 launches | 7,02 ms median; 9,42 ms max; reached in 7/7 launches |
| **Application / base main window initialization** — 7,84 ms median; 11,11 ms max; reached in 7/7 launches | 9,34 ms median; 10,25 ms max; reached in 7/7 launches |
| **Application / IPTVModule** — 7,75 ms median; 11,04 ms max; reached in 7/7 launches | 7,73 ms median; 9,96 ms max; reached in 7/7 launches |
| **Application / VPlayerCoreModule** — 5,84 ms median; 8,41 ms max; reached in 7/7 launches | 7,68 ms median; 94,89 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / repository setup** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 135,00 ms median; 267,38 ms max; reached in 7/7 launches |
| **Application / library Album / repository setup** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 11,85 ms median; 11,85 ms max; reached in 1/7 launches; incomplete sample set |
| **Application / library Album / query** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 330,04 ms median; 330,04 ms max; reached in 1/7 launches; incomplete sample set |
| **Application / library Album / view model construction** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 86,43 ms median; 86,43 ms max; reached in 1/7 launches; incomplete sample set |
| **Application / library Album / UI publication** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 16,58 ms median; 16,58 ms max; reached in 1/7 launches; incomplete sample set |
| **Application / library Album / UI dispatch and publication** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 743,74 ms median; 743,74 ms max; reached in 1/7 launches; incomplete sample set |
| **Application / library Album / post-load callback** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 18,38 ms median; 18,38 ms max; reached in 1/7 launches; incomplete sample set |
| **Application / library Artist / repository setup** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 9,88 ms median; 9,88 ms max; reached in 1/7 launches; incomplete sample set |

## Data

| Baseline now | New optimized version |
| --- | --- |
| **Data / sound items** — 2 640,81 ms median; 3 793,30 ms p95; 8 263,00 ms first; 368,7 MiB allocated | 2 555,95 ms median; 2 695,98 ms p95; 5 020,58 ms first; 368,7 MiB allocated; 3,2% faster |
| **Data / albums** — 13,08 ms median; 19,82 ms p95; 58,70 ms first; 1,4 MiB allocated | 13,20 ms median; 16,60 ms p95; 48,19 ms first; 1,4 MiB allocated; 1,0% slower |
| **Data / artists** — 7,83 ms median; 10,16 ms p95; 18,24 ms first; 0,1 MiB allocated | 7,06 ms median; 8,80 ms p95; 13,59 ms first; 0,1 MiB allocated; 9,9% faster |

## Playlist

| Baseline now | New optimized version |
| --- | --- |
| **Playlist / 100000 entries** — 22 145,50 ms median; 27 065,15 ms p95; 25 917,40 ms first; 811,7 MiB allocated | 2 732,90 ms median; 2 796,62 ms p95; 2 804,04 ms first; 806,4 MiB allocated; 87,7% faster |
| **Playlist / 10000 entries** — 1 009,48 ms median; 2 145,33 ms p95; 1 028,93 ms first; 82,1 MiB allocated | 263,87 ms median; 381,20 ms p95; 352,61 ms first; 81,6 MiB allocated; 73,9% faster |
| **Playlist / 1000 entries** — 515,15 ms median; 606,56 ms p95; 5 029,82 ms first; 8,5 MiB allocated | 52,48 ms median; 139,35 ms p95; 1 507,72 ms first; 8,5 MiB allocated; 89,8% faster |
| **Playlist / summaries** — 9,54 ms median; 10,14 ms p95; 75,27 ms first; 0,3 MiB allocated | 8,84 ms median; 9,25 ms p95; 674,11 ms first; 0,3 MiB allocated; 7,3% faster |

## UI

| Baseline now | New optimized version |
| --- | --- |
| **UI / title sort** — 1 677,83 ms median; 2 473,30 ms p95; 1 894,95 ms first; 5,5 MiB allocated | 1 203,79 ms median; 1 410,83 ms p95; 1 132,07 ms first; 5,5 MiB allocated; 28,3% faster |
| **UI / playlist view models** — 153,31 ms median; 342,36 ms p95; 142,62 ms first; 39,5 MiB allocated | 193,91 ms median; 257,41 ms p95; 158,72 ms first; 39,5 MiB allocated; 26,5% slower |
| **UI / title search** — 47,60 ms median; 49,76 ms p95; 78,93 ms first; 0,1 MiB allocated | 38,37 ms median; 40,22 ms p95; 54,26 ms first; 0,1 MiB allocated; 19,4% faster |
| **UI / virtualized list scroll** — 8,28 ms median; 10,46 ms p95; 11,19 ms first; 0,0 MiB allocated | 7,40 ms median; 11,32 ms p95; 13,46 ms first; 0,0 MiB allocated; 10,6% faster |
| **UI / virtualized list layout** — 6,36 ms median; 330,16 ms p95; 394,69 ms first; 0,0 MiB allocated | 5,97 ms median; 6,07 ms p95; 92,38 ms first; 0,0 MiB allocated; 6,2% faster |

## Lyrics

| Baseline now | New optimized version |
| --- | --- |
| **Lyrics / 100000 lines** — 3,74 ms median; 4,27 ms p95; 18,90 ms first; 0,0 MiB allocated | 3,10 ms median; 3,77 ms p95; 16,38 ms first; 0,0 MiB allocated; 17,0% faster |
| **Lyrics / 10000 lines** — 2,84 ms median; 3,56 ms p95; 288,78 ms first; 0,0 MiB allocated | 1,90 ms median; 2,41 ms p95; 3,42 ms first; 0,0 MiB allocated; 33,3% faster |
| **Lyrics / 1000 lines** — 1,77 ms median; 2,02 ms p95; 14,95 ms first; 0,0 MiB allocated | 1,64 ms median; 1,76 ms p95; 9,40 ms first; 0,0 MiB allocated; 7,6% faster |

## Spectrum

| Baseline now | New optimized version |
| --- | --- |
| **Spectrum / changing frames** — 11,78 ms median; 14,64 ms p95; 15,95 ms first; 0,2 MiB allocated | 3,70 ms median; 4,22 ms p95; 4,43 ms first; 0,2 MiB allocated; 68,6% faster |
| **Spectrum / stable frames** — 5,99 ms median; 18,51 ms p95; 236,76 ms first; 0,2 MiB allocated | 1,45 ms median; 19,64 ms p95; 40,06 ms first; 0,2 MiB allocated; 75,8% faster |

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

This comparison includes playlist loading, the startup crash repair, and library query deferral. Other feature metrics are controls: timing differences in unchanged code are observations and must not be credited to those changes.

Each feature metric is one operation except lyrics (10,000 seeks) and spectrum (1,000 frames). Divide batched results by their operation count before ranking across categories. UI list scenarios use an offscreen text-row ListBox, not application card templates. First samples share one process, so only the first metric includes process-cold EF initialization. Startup uses fresh processes with warm OS caches and fresh default settings; it does not include every background service becoming ready. Startup phase scopes overlap. Allocation counts cover managed allocations across threads and exclude native bitmap/database memory.

See README.md for workload boundaries and the optimization protocol.
