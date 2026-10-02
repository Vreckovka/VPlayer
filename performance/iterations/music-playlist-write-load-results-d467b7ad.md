# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 3k ms | 1.8k ms (-40.7%) |
| **load and render** — 23.1k ms | 26.4k ms (+13.9%) |
| **stored song enrichment** — 2.4k ms | 3.9k ms (+63.3%) |
| **collection publication** — 9.1k ms | 9.5k ms (+5.4%) |
| **collection replacement** — 5.2k ms | 4.7k ms (-9%) |
| **incoming song view conversion** — 3.81 ms | 3.74 ms (-2%) |
| **saved playlist view creation** — 5.9k ms | 5.7k ms (-2.2%) |
| **active item dispatch** — 4.2k ms | 4.4k ms (+5.4%) |
| **database and incoming views** — 3.5k ms | 3.1k ms (-10.2%) |
| **stored song read** — 2.1k ms | 3k ms (+40%) |
| **activation and render** — 1.7k ms | 1.6k ms (-4.4%) |
| **scroll to last track** — 191.5 ms | 570.8 ms (+198%) |
| **long no-match search and render** — 510.5 ms | 743.7 ms (+45.7%) |
| **long near-match search and render** — 453.5 ms | 767.4 ms (+69.2%) |

Compared complete painted runs against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
