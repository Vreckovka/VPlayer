# Buffered UI performance

207k fully played sound items; 5,310 playlists. Buffered diagnostics; median times. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **statistics / load and render** — 5.8k ms | 4.6k ms (-21.4%) |
| **initial playlist view ready** — 4.9k ms | 4.7k ms (-4.7%) |
| **first window render** — 2.7k ms | 2.7k ms (+0.2%) |

Comparisons use the same buffered diagnostics. Slow outliers remain; full samples and provenance stay in JSON.
