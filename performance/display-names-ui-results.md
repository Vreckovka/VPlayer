# Playlist display-name comparison

207k fully played sound items; 5,310 playlists. Median times; - % = less time, + % = more time.

## First comparison

| Baseline now | New optimized version |
| --- | --- |
| **initial playlist view ready** — 4.7k ms | 4.7k ms (-0.1%) |
| **statistics / load and render** — 3.6k ms | 4.6k ms (+28.6%) |
| **first window render** — 2.7k ms | 2.6k ms (-2.8%) |

## Alternating confirmation

| Baseline now | New optimized version |
| --- | --- |
| **initial playlist view ready** — 5.2k ms* | 4.9k ms (-6.2%) |
| **statistics / load and render** — 4k ms* | 3.7k ms (-6.7%) |
| **first window render** — 2.9k ms* | 3.1k ms (+9.2%) |

* Completed control timings; one control launch timed out during VLC initialization.

Statistics changes direction between comparisons. A consistent performance gain from this change is unproven. All samples, including the timeout and slow outliers, are retained in JSON.
