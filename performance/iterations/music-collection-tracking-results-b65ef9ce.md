# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 422.1 ms | 485.2 ms (+15%) |
| **load and render** — 13.9k ms | 10.7k ms (-22.7%) |
| **stored song enrichment** — 3.1k ms | 3.4k ms (+8.8%) |
| **collection publication** — 4.6k ms | 498.1 ms (-89.2%) |
| **collection replacement** — 4.4k ms | 278.5 ms (-93.7%) |
| **incoming song view conversion** — 4.01 ms | 3.38 ms (-15.7%) |
| **saved playlist view creation** — 117.8 ms | 142.7 ms (+21.2%) |
| **active item dispatch** — 199.4 ms | 196.6 ms (-1.4%) |
| **database and incoming views** — 3.1k ms | 3.5k ms (+13.4%) |
| **stored song read** — 2.5k ms | 2.5k ms (+1.5%) |
| **activation and render** — 1.2k ms | 1.4k ms (+19.8%) |
| **scroll to last track** — 412.1 ms | 382.9 ms (-7.1%) |
| **long no-match search and render** — 616.8 ms | 705 ms (+14.3%) |
| **long near-match search and render** — 796 ms | 689.2 ms (-13.4%) |

Completed endpoint timings compare against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
