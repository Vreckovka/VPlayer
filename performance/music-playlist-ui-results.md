# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 662.7 ms | 436.5 ms (-34.1%) |
| **load and render** — 32.4k ms | 19.8k ms (-38.8%) |
| **stored song enrichment** — 2.3k ms | 3.3k ms (+43.1%) |
| **collection publication** — 15.2k ms | 4.8k ms (-68.5%) |
| **collection replacement** — 11.3k ms | 4.5k ms (-59.8%) |
| **incoming song view conversion** — 4.8k ms | 3.16 ms (-99.9%) |
| **saved playlist view creation** — 4.6k ms | 5k ms (+8.5%) |
| **active item dispatch** — 3.8k ms | 201.2 ms (-94.7%) |
| **database and incoming views** — 3.2k ms | 3.5k ms (+10.2%) |
| **stored song read** — 2k ms | 2.4k ms (+18%) |
| **activation and render** — 2.8k ms | 1.7k ms (-38.3%) |
| **scroll to last track** — 222.3 ms | 369.4 ms (+66.2%) |
| **long no-match search and render** — 474.5 ms | 701.7 ms (+47.9%) |
| **long near-match search and render** — 483.8 ms | 690.8 ms (+42.8%) |

Compared complete painted runs against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
