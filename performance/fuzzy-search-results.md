# Library fuzzy search

Full copied library; longest-title queries. Production filtering and result publication; no loading or XAML. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **long near-match / first search** — 13.3k ms | 39.5 ms (-99.7%) |
| **long near-match / repeat search** — 12.9k ms | 40.1 ms (-99.7%) |
| **long no-match / repeat search** — 12.7k ms | 40.8 ms (-99.7%) |
| **long no-match / first search** — 12.4k ms | 42 ms (-99.7%) |

First samples stay separate from repeats. Raw timings, allocations, matching result hashes and provenance stay in JSON.
