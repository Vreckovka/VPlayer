# Worst-case performance

207k sound items; stress playlists up to 100k entries. Median timings; **- % = less time**, **+ % = more time**. Failed or missing baselines have no percentage.

## Application startup

| Baseline now | New optimized version |
| --- | --- |
| **initial playlist view ready** — 5.8k ms | 4.7k ms (-18.6%) |
| **first window render** — Failed | 3.2k ms |
| **First render after crash fix** — 3.5k ms | 3.2k ms (-7.5%) |

<details>
<summary>Startup breakdown</summary>

| Baseline now | New optimized version |
| --- | --- |
| **initialization** — 1.9k ms | 1.6k ms (-15.8%) |
| **main window view model initialization** — 998.4 ms | 529.1 ms (-47%) |
| **shell construction** — 927.8 ms | 648.8 ms (-30.1%) |
| **module registration** — 871.2 ms | 713.5 ms (-18.1%) |
| **WindowsPlayerNinjectModule** — 795.1 ms | 642.6 ms (-19.2%) |
| **Playlists / UI dispatch and publication** — 760.8 ms | 256.8 ms (-66.2%) |
| **navigation view model construction** — 746.9 ms | 224.8 ms (-69.9%) |
| **show shell** — 722.5 ms | 945.1 ms (+30.8%) |
| **video player activation** — 568.9 ms | 457.2 ms (-19.6%) |
| **Music playlists / construction** — 534.5 ms | 15.1 ms (-97.2%) |
| **Playlists / post-load callback** — 213.9 ms | 214.9 ms (+0.5%) |
| **Playlists / playlist preparation** — 186.7 ms | 186.5 ms (-0.1%) |
| **Playlists / query** — 178.6 ms | 809.2 ms (+353.1%) |
| **player controls activation** — 109.8 ms | 134.3 ms (+22.2%) |
| **music player activation** — 94.8 ms | 97.1 ms (+2.4%) |
| **navigation activation** — 84.8 ms | 104.6 ms (+23.3%) |
| **Playlists / current track metadata** — 77.6 ms | 63.1 ms (-18.7%) |
| **benchmark first-frame screenshot** — 76.4 ms | 82.2 ms (+7.5%) |
| **main window XAML** — 66.5 ms | 64 ms (-3.7%) |
| **library module** — 58.9 ms | 59.1 ms (+0.3%) |
| **Playlists / view model construction** — 53.5 ms | 82 ms (+53.2%) |
| **container activation** — 48 ms | 42.2 ms (-12%) |
| **Playlists / pinned items** — 35.4 ms | 34.6 ms (-2.5%) |
| **Artists / construction** — 35.3 ms | 10.9 ms (-69.2%) |
| **settings** — 22.5 ms | 21.6 ms (-3.8%) |
| **OpenCV native initialization** — 21.4 ms | 23.5 ms (+9.7%) |
| **audio device enumeration** — 21.3 ms | 21.3 ms (+0.3%) |
| **player controls construction** — 18.5 ms | 20.3 ms (+9.7%) |
| **File browser / construction** — 17.1 ms | 19.5 ms (+13.9%) |
| **Settings / construction** — 15.7 ms | 14.6 ms (-6.9%) |
| **Albums / construction** — 13.5 ms | 13.2 ms (-2.3%) |
| **Playlists / UI publication** — 12.9 ms | 13.5 ms (+4.6%) |
| **Cloud / construction** — 12.9 ms | 14.6 ms (+12.9%) |
| **Video playlists / construction** — 12.3 ms | 12.2 ms (-1.3%) |
| **TV shows / construction** — 11.6 ms | 11.6 ms (-0.7%) |
| **Statistics / construction** — 11.2 ms | 12.2 ms (+8.9%) |
| **UPnP / construction** — 9.22 ms | 10.7 ms (+16.3%) |
| **UPnPNinjectModule** — 8.9 ms | 9.58 ms (+7.7%) |
| **base main window initialization** — 7.84 ms | 8.78 ms (+11.9%) |
| **IPTVModule** — 7.75 ms | 9.31 ms (+20.1%) |
| **VPlayerCoreModule** — 5.84 ms | 7.3 ms (+25%) |
| **Playlists / repository setup** — n/a | 199.5 ms |
| **initial playlist view ready before query deferral** — 5.1k ms | 4.7k ms (-8%) |
| **first window render before query deferral** — 3.5k ms | 3.2k ms (-8.2%) |

</details>

## Data

| Baseline now | New optimized version |
| --- | --- |
| **sound items** — 2.6k ms | 3.8k ms (+44.2%) |
| **albums** — 13.1 ms | 17.7 ms (+35.7%) |
| **artists** — 7.83 ms | 8.98 ms (+14.7%) |

### Statistics (207k unique file records)

| Baseline now | New optimized version |
| --- | --- |
| **first data load** — 4.4k ms | 1.9k ms (-55.9%) |
| **warm data load** — 2.9k ms | 299.4 ms (-89.5%) |

### Statistics (fully played, 207k unique times)

| Baseline now | New optimized version |
| --- | --- |
| **first data load** — 1.7k ms | 1.7k ms (+1.4%) |
| **warm data load** — 507.9 ms | 514 ms (+1.2%) |

## Playlist

| Baseline now | New optimized version |
| --- | --- |
| **100k entries** — 22.1k ms | 3.1k ms (-86.1%) |
| **10k entries** — 1k ms | 336.3 ms (-66.7%) |
| **1k entries** — 515.2 ms | 59.9 ms (-88.4%) |
| **summaries** — 9.54 ms | 13.3 ms (+39.9%) |

## UI

| Baseline now | New optimized version |
| --- | --- |
| **title sort** — 1.7k ms | 1.3k ms (-23.1%) |
| **playlist view models** — 153.3 ms | 212.4 ms (+38.5%) |
| **title search** — 47.6 ms | 41.7 ms (-12.4%) |
| **virtualized list scroll** — 8.28 ms | 7.44 ms (-10.1%) |
| **virtualized list layout** — 6.36 ms | 8.16 ms (+28.2%) |

### Statistics (207k unique file records)

| Baseline now | New optimized version |
| --- | --- |
| **load and render** — 6.2k ms | 2.6k ms (-57.3%) |

### Statistics (fully played, 207k unique times)

| Baseline now | New optimized version |
| --- | --- |
| **load and render** — 3.5k ms | 4.2k ms (+20.3%) |

### Grouped playlists (5k added favorites)

| Baseline now | New optimized version |
| --- | --- |
| Populated view: Timeout (60k ms) | 5.6k ms |
| First frame*: 3.6k ms | 3.2k ms (-10.8%) |
| Scroll to last favorite: n/a | 280 ms |
| Realized rows (ready / scrolled): n/a / n/a | 41 / 41 |

## Lyrics (10k seeks)

| Baseline now | New optimized version |
| --- | --- |
| **100k lines** — 3.74 ms | 3.66 ms (-2.1%) |
| **10k lines** — 2.84 ms | 2.15 ms (-24.5%) |
| **1k lines** — 1.77 ms | 2 ms (+13.1%) |

## Spectrum (1k frames)

| Baseline now | New optimized version |
| --- | --- |
| **changing frames** — 11.8 ms | 4.34 ms (-63.2%) |
| **stable frames** — 5.99 ms | 2.06 ms (-65.7%) |

## Still to measure

Library cards/scrolling; navigation/details; file browser/thumbnails; settings/dialogs; video/fullscreen; cloud timeouts; full library load/fuzzy filtering.

Percentage changes in unchanged code are observations. Startup phases overlap; component UI tests use a simplified list. Raw samples, maxima, allocations and commit/fixture provenance remain in the JSON files and [README](README.md).
