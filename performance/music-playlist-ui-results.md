# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 662.7 ms | 485.2 ms (-26.8%) |
| **load and render** — 32.4k ms | 10.7k ms (-66.9%) |
| **stored song enrichment** — 2.3k ms | 3.4k ms (+48.8%) |
| **collection publication** — 15.2k ms | 498.1 ms (-96.7%) |
| **collection replacement** — 11.3k ms | 278.5 ms (-97.5%) |
| **incoming song view conversion** — 4.8k ms | 3.38 ms (-99.9%) |
| **saved playlist view creation** — 4.6k ms | 142.7 ms (-96.9%) |
| **active item dispatch** — 3.8k ms | 196.6 ms (-94.8%) |
| **database and incoming views** — 3.2k ms | 3.5k ms (+9.4%) |
| **stored song read** — 2k ms | 2.5k ms (+22.9%) |
| **activation and render** — 2.8k ms | 1.4k ms (-48.4%) |
| **scroll to last track** — 222.3 ms | 382.9 ms (+72.3%) |
| **long no-match search and render** — 474.5 ms | 705 ms (+48.6%) |
| **long near-match search and render** — 483.8 ms | 689.2 ms (+42.5%) |

Completed endpoint timings compare against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
