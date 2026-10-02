# Statistics reload bursts

32 rapid Load requests; 207k fully played items and 5,310 playlists. Median times; - % = less time, + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **reload burst / load and render** — 8.4k ms | 1.2k ms (-85.7%) |
| **first burst / data and UI publication** — 5.8k ms | 1.7k ms (-70.7%) |
| **repeat burst / data and UI publication** — 4.4k ms | 514.5 ms (-88.3%) |

Data rows include production loading and dispatcher publication. The WPF row includes rendering in the actual app. Full samples, failures, repository counts and provenance stay in JSON.
