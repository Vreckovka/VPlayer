# Statistics reload bursts

32 rapid Load requests; 207k fully played items and 5,310 playlists. Median times; - % = less time, + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **reload burst / WPF render** — 8.4k ms | Pending |
| **first burst / data and UI publication** — 5.8k ms | Pending |
| **repeat burst / data and UI publication** — 4.4k ms | Pending |

Data rows include production loading and dispatcher publication. The WPF row includes rendering in the actual app. Full samples, failures, repository counts and provenance stay in JSON.
