# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 1.8k ms | 552.3 ms (-68.5%) |
| **load and render** — 26.4k ms | 24.8k ms (-5.8%) |
| **stored song enrichment** — 3.9k ms | 2.7k ms (-29.4%) |
| **collection publication** — 9.5k ms | 9.1k ms (-4.9%) |
| **collection replacement** — 4.7k ms | 4.7k ms (-1.2%) |
| **incoming song view conversion** — 3.74 ms | 3.32 ms (-11.1%) |
| **saved playlist view creation** — 5.7k ms | 5.3k ms (-7.5%) |
| **active item dispatch** — 4.4k ms | 4.5k ms (+2.3%) |
| **database and incoming views** — 3.1k ms | 3.5k ms (+10.6%) |
| **stored song read** — 3k ms | 2.3k ms (-22.9%) |
| **activation and render** — 1.6k ms | 1.1k ms (-30.2%) |
| **scroll to last track** — 570.8 ms | 458.5 ms (-19.7%) |
| **long no-match search and render** — 743.7 ms | 572.6 ms (-23%) |
| **long near-match search and render** — 767.4 ms | 796.2 ms (+3.8%) |

Compared complete painted runs against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
