# Playlist query preparation experiment

207k fully played sound items; 5,310 playlists. Buffered diagnostics; median times. - % = less time; + % = more time.

| Baseline now | Optional candidate |
| --- | --- |
| **initial playlist view ready** — 4.5k ms | 4.3k ms (-3.9%) |
| **first window render** — 2.5k ms | 2.6k ms (+2.9%) |
| **playlist data query** — 1k ms | 159.7 ms (-84.6%) |

Comparisons use the same buffered diagnostics. Slow outliers remain; full samples and provenance stay in JSON.

Preparation stays disabled by default: the large query reduction produced a small readiness gain and a slower first render. Both series retain their slow outliers.
