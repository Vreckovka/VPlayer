# Worst-case performance

207k sound items; stress playlists up to 100k entries. Median timings; **- % = less time**, **+ % = more time**. Failed or missing baselines have no percentage.

## Application startup

Startup focus/stacking regression: [window ordering check](window-startup-results.md).

Fully played library; buffered diagnostics.

| Baseline now | New optimized version |
| --- | --- |
| **initial playlist view ready** — 4.9k ms | 4.7k ms (-4.9%) |
| **first window render** — 2.7k ms | 2.7k ms (-0.2%) |

<details>
<summary>Earlier startup comparisons</summary>


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

</details>

<details>
<summary>Optional startup experiment — disabled by default</summary>

| Baseline now | Optional candidate |
| --- | --- |
| **initial playlist view ready** — 4.5k ms | 4.3k ms (-3.9%) |
| **first window render** — 2.5k ms | 2.6k ms (+2.9%) |
| **playlist data query** — 1k ms | 159.7 ms (-84.6%) |

Query preparation reduced query time; overall readiness improved slightly and first render slowed. Full evidence: [playlist-preparation-results.md](playlist-preparation-results.md).

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

### 100k-entry music playlist

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 662.7 ms | 583 ms (-12%) |
| **load and render** — 32.4k ms | 15.9k ms (-51%) |
| **stored song enrichment** — 2.3k ms | 4.7k ms (+108.1%) |
| **collection publication** — 15.2k ms | 4.7k ms (-69.4%) |
| **collection replacement** — 11.3k ms | 4.4k ms (-60.9%) |
| **incoming song view conversion** — 4.8k ms | 3.41 ms (-99.9%) |
| **saved playlist view creation** — 4.6k ms | 130.6 ms (-97.2%) |
| **active item dispatch** — 3.8k ms | 217.8 ms (-94.2%) |
| **database and incoming views** — 3.2k ms | 3.5k ms (+10.9%) |
| **stored song read** — 2k ms | 2.5k ms (+22.7%) |
| **activation and render** — 2.8k ms | 2.2k ms (-19.5%) |
| **scroll to last track** — 222.3 ms | 687.2 ms (+209.2%) |
| **long no-match search and render** — 474.5 ms | 972.8 ms (+105%) |
| **long near-match search and render** — 483.8 ms | 827.6 ms (+71.1%) |

Actual WPF load, scrolling and search. Timeout endpoints have no percentage. [Evidence](music-playlist-ui-results.md).
Storage task d467b7ad measured +13.9% load/render against its preceding version; scrolling and search also slowed. [Storage task comparison](iterations/music-playlist-write-load-results-d467b7ad.md).
[Playback-save follow-up](iterations/music-playlist-playback-save-load-results-09ea4d79.md) retains slower stages as well.
[Snapshot task comparison](iterations/music-playlist-snapshot-load-results-2abd4a10.md) retains the earlier snapshot comparison, including slower stages.
[Selection task rendering check](iterations/music-playlist-selection-load-results-ebff741c.md) retains the load timeout and slower scrolling/search.
[Saved-view task comparison](iterations/music-playlist-saved-views-load-results-171b2af1.md): view creation -97%; total load/render -4.7%. Enrichment and publication remain the largest stages; slower observations are retained.

### Playlist state snapshot (100k entries)

| Baseline now | New optimized version |
| --- | --- |
| **first snapshot** — 3.9k ms | 186.7 ms (-95.2%) |
| **repeated snapshot** — 4k ms | 138.4 ms (-96.5%) |
| **allocated per snapshot** — 696.3 MiB | 30.7 MiB (-95.6%) |

Production snapshot used by track selection and playlist loading; component timings exclude database work and rendering. [Evidence](playlist-snapshot-results.md).

### Saved playlist view creation (100k entries)

| Baseline now | New optimized version |
| --- | --- |
| **first batch** — 3.8k ms | 157.1 ms (-95.8%) |
| **repeated batch** — 3.9k ms | 217.9 ms (-94.4%) |
| **allocated per batch** — 1.9 GiB | 57.4 MiB (-97.1%) |

Production music view construction with real Ninject and stub services; excludes database reads and rendering. [Evidence](saved-playlist-views-results.md).

### 100k-track save and clear

| Baseline now | New optimized version |
| --- | --- |
| **Reorder, save and render 100k tracks** — Timed out | 28.6k ms |
| **Save, clear and render 100k tracks** — 66.9k ms | 14.3k ms (-78.6%) |

Persisted occurrences and painted results verified. [Evidence](music-playlist-write-results.md).

### Full library fuzzy search

| Baseline now | New optimized version |
| --- | --- |
| **long near-match / first search** — 13.3k ms | 39.5 ms (-99.7%) |
| **long near-match / repeat search** — 12.9k ms | 40.1 ms (-99.7%) |
| **long no-match / repeat search** — 12.7k ms | 40.8 ms (-99.7%) |
| **long no-match / first search** — 12.4k ms | 42 ms (-99.7%) |

Production filtering and result publication; loading and XAML are measured separately.

### Statistics reload bursts (32 rapid requests)

| Baseline now | New optimized version |
| --- | --- |
| **reload burst / load and render** — 8.4k ms | 1.2k ms (-85.7%) |
| **first burst / data and UI publication** — 5.8k ms | 1.7k ms (-70.7%) |
| **repeat burst / data and UI publication** — 4.4k ms | 514.5 ms (-88.3%) |

Data rows include dispatcher publication; the WPF row includes rendering in the actual app.

### Other UI measurements

| Baseline now | New optimized version |
| --- | --- |
| **title sort** — 1.7k ms | 1.3k ms (-23.1%) |
| **playlist view models** — 153.3 ms | 212.4 ms (+38.5%) |
| **title search** — 47.6 ms | 41.7 ms (-12.4%) |
| **virtualized list scroll** — 8.28 ms | 7.44 ms (-10.1%) |
| **virtualized list layout** — 6.36 ms | 8.16 ms (+28.2%) |

### Statistics (fully played, buffered)

| Baseline now | New optimized version |
| --- | --- |
| **load and render** — 5.8k ms | 3.3k ms (-43.9%) |

<details>
<summary>Earlier Statistics comparisons</summary>


### Statistics (207k unique file records)

| Baseline now | New optimized version |
| --- | --- |
| **load and render** — 6.2k ms | 2.6k ms (-57.3%) |

### Statistics (fully played, 207k unique times)

| Baseline now | New optimized version |
| --- | --- |
| **load and render** — 3.5k ms | 4.2k ms (+20.3%) |

</details>

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

Cold startup after a restart; full application data load; stored album/file details; native rapid next/back and save overhead; lyrics playback/scrolling; explorer loading/finding. See [current scope](focus.md).

## Rapid playlist selection

| Baseline now | New optimized version |
| --- | --- |
| **100 out-of-order skips: stale media applies** — 99 | 0 (-100%) |
| **Clear during preparation: unhandled exceptions** — 1 | 0 (-100%) |
| **Same track in another occurrence: playback starts** — 2 | 1 (-50%) |
| **Previous from first: selected index** — 100k (invalid) | 99999 (last item) |

100k-entry controlled correctness checks; mocked media, no timing claim. [Details](playback-selection-results.md).

Percentage changes in unchanged code are observations. Startup phases overlap; component UI tests use a simplified list. Raw samples, maxima, allocations and commit/fixture provenance remain in the JSON files and [README](README.md).

Startup and single Statistics-load timings compare the original buffered baseline with the retained c7894170 series. [Playlist-name task comparisons](display-names-ui-results.md) vary in direction; a consistent gain from that change is unproven. Raw samples retain the control timeout and slow outliers.
