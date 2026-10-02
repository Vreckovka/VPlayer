# Worst-case performance comparison

Baseline commit: 36bd8be4ea300705452da308032c31ee45b81ed2. Optimized commit: c2558db9c7f4a6554e6ed723c518750a8e69cf0b.
Workload: 207110 sound items, 310 playlists, plus 1,000 / 10,000 / 100,000-entry stress playlists. Release x64 on .NET 3.1.32.
Fixture SHA-256: 89737F48D5184B0C3517FCD06872366D11918C621B7005589EEEADE6DC4611AE. Baseline samples are immutable.

## Application startup

Startup comparison commit: c2558db9c7f4a6554e6ed723c518750a8e69cf0b.
The initial populated-view milestone has a separate baseline at f9bdaa8a4b6c5e1612965d5759179e160b6833b7 after repairing the startup crash. Earlier failed launches cannot provide this timing.
Additional startup phases use the first recorded baseline containing that phase. Baseline commits: ed0d248e3d8e20322d6672c3262c08ded293a0b1, 94ed1e6123d8a25aef29bf8dd0a4b1e9e3adccb9. These were recorded before query deferral.

| Baseline now | New optimized version |
| --- | --- |
| **Application / initial playlist view ready** — 5 774,52 ms median; 6 897,59 ms max; reached in 7/7 launches | 4 700,49 ms median; 9 626,29 ms max; reached in 7/7 launches |
| **Application / initial playlist view ready before query deferral** — 5 108,71 ms median; 10 111,84 ms max; reached in 7/7 launches | 4 700,49 ms median; 9 626,29 ms max; reached in 7/7 launches |
| **Application / first window render** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 3 239,62 ms median; 6 979,17 ms max; reached in 7/7 launches |
| **Application / first window render before query deferral** — 3 528,59 ms median; 7 749,60 ms max; reached in 7/7 launches | 3 239,62 ms median; 6 979,17 ms max; reached in 7/7 launches |
| **Application / first window render after crash fix** — 3 503,49 ms median; 5 118,84 ms max; reached in 7/7 launches | 3 239,62 ms median; 6 979,17 ms max; reached in 7/7 launches |
| **Application / initialization** — 1 915,51 ms median; 2 211,14 ms max; reached in 7/7 launches | 1 611,96 ms median; 3 133,78 ms max; reached in 7/7 launches |
| **Application / main window view model initialization** — 998,39 ms median; 1 266,93 ms max; reached in 7/7 launches | 529,11 ms median; 753,01 ms max; reached in 7/7 launches |
| **Application / shell construction** — 927,84 ms median; 1 124,91 ms max; reached in 7/7 launches | 648,76 ms median; 953,91 ms max; reached in 7/7 launches |
| **Application / module registration** — 871,23 ms median; 1 226,52 ms max; reached in 7/7 launches | 713,45 ms median; 1 992,71 ms max; reached in 7/7 launches |
| **Application / WindowsPlayerNinjectModule** — 795,11 ms median; 1 179,31 ms max; reached in 7/7 launches | 642,58 ms median; 912,93 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / UI dispatch and publication** — 760,78 ms median; 1 161,19 ms max; reached in 7/7 launches | 256,78 ms median; 1 198,99 ms max; reached in 7/7 launches |
| **Application / navigation view model construction** — 746,87 ms median; 875,11 ms max; reached in 7/7 launches | 224,77 ms median; 334,31 ms max; reached in 7/7 launches |
| **Application / show shell** — 722,53 ms median; 1 012,92 ms max; reached in 7/7 launches | 945,08 ms median; 1 097,12 ms max; reached in 7/7 launches |
| **Application / video player activation** — 568,91 ms median; 674,25 ms max; reached in 7/7 launches | 457,18 ms median; 505,49 ms max; reached in 7/7 launches |
| **Application / library SoundItemPlaylistsViewModel / navigation construction** — 534,46 ms median; 634,59 ms max; reached in 7/7 launches | 15,15 ms median; 23,01 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / post-load callback** — 213,90 ms median; 293,86 ms max; reached in 7/7 launches | 214,87 ms median; 659,16 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / playlist preparation** — 186,74 ms median; 277,25 ms max; reached in 7/7 launches | 186,50 ms median; 631,20 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / query** — 178,60 ms median; 1 253,93 ms max; reached in 7/7 launches | 809,18 ms median; 1 508,74 ms max; reached in 7/7 launches |
| **Application / player controls activation** — 109,83 ms median; 152,29 ms max; reached in 7/7 launches | 134,25 ms median; 234,08 ms max; reached in 7/7 launches |
| **Application / music player activation** — 94,80 ms median; 479,84 ms max; reached in 7/7 launches | 97,07 ms median; 230,66 ms max; reached in 7/7 launches |
| **Application / navigation activation** — 84,82 ms median; 125,73 ms max; reached in 7/7 launches | 104,57 ms median; 111,94 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / current track metadata** — 77,64 ms median; 118,04 ms max; reached in 7/7 launches | 63,11 ms median; 111,40 ms max; reached in 7/7 launches |
| **Application / benchmark first-frame screenshot** — 76,44 ms median; 175,27 ms max; reached in 7/7 launches | 82,20 ms median; 101,85 ms max; reached in 7/7 launches |
| **Application / main window XAML** — 66,49 ms median; 106,70 ms max; reached in 7/7 launches | 64,04 ms median; 149,61 ms max; reached in 7/7 launches |
| **Application / library module** — 58,91 ms median; 85,93 ms max; reached in 7/7 launches | 59,08 ms median; 72,18 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / view model construction** — 53,53 ms median; 116,66 ms max; reached in 7/7 launches | 82,02 ms median; 127,08 ms max; reached in 7/7 launches |
| **Application / container activation** — 48,01 ms median; 73,33 ms max; reached in 7/7 launches | 42,22 ms median; 471,08 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / pinned items** — 35,43 ms median; 45,44 ms max; reached in 7/7 launches | 34,55 ms median; 46,91 ms max; reached in 7/7 launches |
| **Application / library IArtistsViewModel / navigation construction** — 35,30 ms median; 45,64 ms max; reached in 7/7 launches | 10,88 ms median; 13,08 ms max; reached in 7/7 launches |
| **Application / settings** — 22,48 ms median; 29,24 ms max; reached in 7/7 launches | 21,63 ms median; 451,62 ms max; reached in 7/7 launches |
| **Application / OpenCV native initialization** — 21,41 ms median; 29,61 ms max; reached in 7/7 launches | 23,49 ms median; 31,22 ms max; reached in 7/7 launches |
| **Application / audio device enumeration** — 21,27 ms median; 40,40 ms max; reached in 7/7 launches | 21,33 ms median; 24,37 ms max; reached in 7/7 launches |
| **Application / player controls construction** — 18,53 ms median; 24,78 ms max; reached in 7/7 launches | 20,33 ms median; 21,04 ms max; reached in 7/7 launches |
| **Application / library WindowsFileBrowserViewModel / navigation construction** — 17,10 ms median; 23,44 ms max; reached in 7/7 launches | 19,47 ms median; 33,76 ms max; reached in 7/7 launches |
| **Application / library SettingsViewModel / navigation construction** — 15,70 ms median; 19,55 ms max; reached in 7/7 launches | 14,61 ms median; 14,97 ms max; reached in 7/7 launches |
| **Application / library IAlbumsViewModel / navigation construction** — 13,50 ms median; 16,86 ms max; reached in 7/7 launches | 13,18 ms median; 15,04 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / UI publication** — 12,89 ms median; 19,36 ms max; reached in 7/7 launches | 13,48 ms median; 25,12 ms max; reached in 7/7 launches |
| **Application / library PCloudManagerViewModel / navigation construction** — 12,89 ms median; 15,42 ms max; reached in 7/7 launches | 14,55 ms median; 19,89 ms max; reached in 7/7 launches |
| **Application / library VideoPlaylistsViewModel / navigation construction** — 12,34 ms median; 14,67 ms max; reached in 7/7 launches | 12,18 ms median; 16,87 ms max; reached in 7/7 launches |
| **Application / library TvShowsViewModel / navigation construction** — 11,64 ms median; 14,30 ms max; reached in 7/7 launches | 11,56 ms median; 13,46 ms max; reached in 7/7 launches |
| **Application / library StatisticsViewModel / navigation construction** — 11,16 ms median; 19,42 ms max; reached in 7/7 launches | 12,16 ms median; 25,83 ms max; reached in 7/7 launches |
| **Application / library UPnPManagerViewModel / navigation construction** — 9,22 ms median; 11,01 ms max; reached in 7/7 launches | 10,72 ms median; 13,33 ms max; reached in 7/7 launches |
| **Application / UPnPNinjectModule** — 8,90 ms median; 11,76 ms max; reached in 7/7 launches | 9,58 ms median; 11,04 ms max; reached in 7/7 launches |
| **Application / base main window initialization** — 7,84 ms median; 11,11 ms max; reached in 7/7 launches | 8,78 ms median; 11,32 ms max; reached in 7/7 launches |
| **Application / IPTVModule** — 7,75 ms median; 11,04 ms max; reached in 7/7 launches | 9,31 ms median; 10,67 ms max; reached in 7/7 launches |
| **Application / VPlayerCoreModule** — 5,84 ms median; 8,41 ms max; reached in 7/7 launches | 7,30 ms median; 135,49 ms max; reached in 7/7 launches |
| **Application / library SoundItemFilePlaylist / repository setup** — Not reached; 7/7 failed launches (Failed: System.NullReferenceException) | 199,46 ms median; 304,00 ms max; reached in 7/7 launches |

## Data

| Baseline now | New optimized version |
| --- | --- |
| **Data / sound items** — 2 640,81 ms median; 3 793,30 ms p95; 8 263,00 ms first; 368,7 MiB allocated | 3 807,53 ms median; 4 323,09 ms p95; 4 200,89 ms first; 368,7 MiB allocated; 44,2% slower |
| **Data / albums** — 13,08 ms median; 19,82 ms p95; 58,70 ms first; 1,4 MiB allocated | 17,74 ms median; 22,52 ms p95; 38,03 ms first; 1,4 MiB allocated; 35,7% slower |
| **Data / artists** — 7,83 ms median; 10,16 ms p95; 18,24 ms first; 0,1 MiB allocated | 8,98 ms median; 10,19 ms p95; 18,03 ms first; 0,1 MiB allocated; 14,7% slower |

## Playlist

| Baseline now | New optimized version |
| --- | --- |
| **Playlist / 100000 entries** — 22 145,50 ms median; 27 065,15 ms p95; 25 917,40 ms first; 811,7 MiB allocated | 3 083,35 ms median; 3 747,30 ms p95; 3 067,10 ms first; 806,4 MiB allocated; 86,1% faster |
| **Playlist / 10000 entries** — 1 009,48 ms median; 2 145,33 ms p95; 1 028,93 ms first; 82,1 MiB allocated | 336,29 ms median; 431,51 ms p95; 356,06 ms first; 81,6 MiB allocated; 66,7% faster |
| **Playlist / 1000 entries** — 515,15 ms median; 606,56 ms p95; 5 029,82 ms first; 8,5 MiB allocated | 59,91 ms median; 64,74 ms p95; 1 419,61 ms first; 8,5 MiB allocated; 88,4% faster |
| **Playlist / summaries** — 9,54 ms median; 10,14 ms p95; 75,27 ms first; 0,3 MiB allocated | 13,34 ms median; 16,16 ms p95; 49,93 ms first; 0,3 MiB allocated; 39,9% slower |

## UI

| Baseline now | New optimized version |
| --- | --- |
| **UI / title sort** — 1 677,83 ms median; 2 473,30 ms p95; 1 894,95 ms first; 5,5 MiB allocated | 1 290,21 ms median; 1 326,26 ms p95; 1 309,28 ms first; 5,5 MiB allocated; 23,1% faster |
| **UI / playlist view models** — 153,31 ms median; 342,36 ms p95; 142,62 ms first; 39,5 MiB allocated | 212,38 ms median; 240,84 ms p95; 687,73 ms first; 39,5 MiB allocated; 38,5% slower |
| **UI / title search** — 47,60 ms median; 49,76 ms p95; 78,93 ms first; 0,1 MiB allocated | 41,69 ms median; 48,66 ms p95; 65,39 ms first; 0,1 MiB allocated; 12,4% faster |
| **UI / virtualized list scroll** — 8,28 ms median; 10,46 ms p95; 11,19 ms first; 0,0 MiB allocated | 7,44 ms median; 12,00 ms p95; 12,05 ms first; 0,0 MiB allocated; 10,1% faster |
| **UI / virtualized list layout** — 6,36 ms median; 330,16 ms p95; 394,69 ms first; 0,0 MiB allocated | 8,16 ms median; 9,33 ms p95; 114,30 ms first; 0,0 MiB allocated; 28,2% slower |

### Grouped music playlists — actual application

Separate worst-case fixture: 207,110 sound items and 5,310 playlists, including 5,000 additional favorites with long titles. Seven visible fresh-profile launches per version. The baseline failed to become usable within 60 seconds; times from its first frame are partial observations.

| Baseline now | New optimized version |
| --- | --- |
| Complete render and last-favorite scroll: 0/7 completed; 7 timed out at 60 s; 0 failed | 7/7 completed; 0 timed out at 60 s; 0 failed |
| Populated playlist view: Not reached (0/7) | 5 576,12 ms median; 7 895,76 ms max; 7/7 reached |
| First window render: 3 631,23 ms median; 4 016,86 ms max; 7/7 reached | 3 237,56 ms median; 4 881,67 ms max; 7/7 reached |
| Scroll to last favorite: Not reached (0/7) | 280,00 ms median; 435,53 ms max; 7/7 reached |
| Realized playlist rows at ready: Not reached | 41 median; 41 max |
| Realized playlist rows after scroll: Not reached | 41 median; 41 max |
| Commit: 64a18e1fcecbc33ccf3254968ca5975d6de2bd3b | c2558db9c7f4a6554e6ed723c518750a8e69cf0b |

See [grouped-ui-results.md](grouped-ui-results.md) for fixture identity, benchmark boundaries and complete scenario notes.

## Lyrics

| Baseline now | New optimized version |
| --- | --- |
| **Lyrics / 100000 lines** — 3,74 ms median; 4,27 ms p95; 18,90 ms first; 0,0 MiB allocated | 3,66 ms median; 4,47 ms p95; 24,09 ms first; 0,0 MiB allocated; 2,1% faster |
| **Lyrics / 10000 lines** — 2,84 ms median; 3,56 ms p95; 288,78 ms first; 0,0 MiB allocated | 2,15 ms median; 2,75 ms p95; 3,45 ms first; 0,0 MiB allocated; 24,5% faster |
| **Lyrics / 1000 lines** — 1,77 ms median; 2,02 ms p95; 14,95 ms first; 0,0 MiB allocated | 2,00 ms median; 2,35 ms p95; 12,17 ms first; 0,0 MiB allocated; 13,1% slower |

## Spectrum

| Baseline now | New optimized version |
| --- | --- |
| **Spectrum / changing frames** — 11,78 ms median; 14,64 ms p95; 15,95 ms first; 0,2 MiB allocated | 4,34 ms median; 6,16 ms p95; 4,31 ms first; 0,2 MiB allocated; 63,2% faster |
| **Spectrum / stable frames** — 5,99 ms median; 18,51 ms p95; 236,76 ms first; 0,2 MiB allocated | 2,06 ms median; 5,86 ms p95; 50,68 ms first; 0,2 MiB allocated; 65,7% faster |

## Coverage still requiring end-to-end scenarios

| Baseline now | New optimized version |
| --- | --- |
| **Library card templates and scrolling** — Not measured yet | Pending |
| **Navigation and detail views** — Not measured yet | Pending |
| **File-browser folders and thumbnails** — Not measured yet | Pending |
| **Settings and modal dialogs** — Not measured yet | Pending |
| **Video and fullscreen transitions** — Not measured yet | Pending |
| **Cloud and network timeout handling** — Not measured yet | Pending |
| **LibraryCollection full sound-item load/fuzzy filter publication** — Not measured yet | Pending |

This comparison includes playlist loading, the startup crash repair, library query deferral, and actual grouped-playlist UI virtualization. Other feature metrics are controls: timing differences in unchanged code are observations and must not be credited to those changes.

Each feature metric is one operation except lyrics (10,000 seeks) and spectrum (1,000 frames). Divide batched results by their operation count before ranking across categories. Component UI list scenarios use an offscreen text-row ListBox, not application card templates. First samples share one process, so only the first metric includes process-cold EF initialization. Startup uses fresh processes with warm OS caches and fresh default settings; it does not include every background service becoming ready. Startup phase scopes overlap. Allocation counts cover managed allocations across threads and exclude native bitmap/database memory.

See README.md for workload boundaries and the optimization protocol.
