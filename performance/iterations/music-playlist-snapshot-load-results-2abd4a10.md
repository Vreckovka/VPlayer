# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 552.3 ms | 436.5 ms (-21%) |
| **load and render** — 24.8k ms | 19.8k ms (-20.2%) |
| **stored song enrichment** — 2.7k ms | 3.3k ms (+18.4%) |
| **collection publication** — 9.1k ms | 4.8k ms (-47.3%) |
| **collection replacement** — 4.7k ms | 4.5k ms (-2.6%) |
| **incoming song view conversion** — 3.32 ms | 3.16 ms (-5%) |
| **saved playlist view creation** — 5.3k ms | 5k ms (-5%) |
| **active item dispatch** — 4.5k ms | 201.2 ms (-95.5%) |
| **database and incoming views** — 3.5k ms | 3.5k ms (+1.2%) |
| **stored song read** — 2.3k ms | 2.4k ms (+6%) |
| **activation and render** — 1.1k ms | 1.7k ms (+50.8%) |
| **scroll to last track** — 458.5 ms | 369.4 ms (-19.4%) |
| **long no-match search and render** — 572.6 ms | 701.7 ms (+22.5%) |
| **long near-match search and render** — 796.2 ms | 690.8 ms (-13.2%) |

Compared complete painted runs against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
