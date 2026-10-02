# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 662.7 ms | 3k ms (+346.4%) |
| **load and render** — 32.4k ms | 23.1k ms (-28.5%) |
| **stored song enrichment** — 2.3k ms | 2.4k ms (+4.9%) |
| **collection publication** — 15.2k ms | 9.1k ms (-40.4%) |
| **collection replacement** — 11.3k ms | 5.2k ms (-54%) |
| **incoming song view conversion** — 4.8k ms | 3.81 ms (-99.9%) |
| **saved playlist view creation** — 4.6k ms | 5.9k ms (+26.2%) |
| **active item dispatch** — 3.8k ms | 4.2k ms (+10.6%) |
| **database and incoming views** — 3.2k ms | 3.5k ms (+9.6%) |
| **stored song read** — 2k ms | 2.1k ms (+3.3%) |
| **activation and render** — 2.8k ms | 1.7k ms (-38.7%) |
| **scroll to last track** — 222.3 ms | 191.5 ms (-13.8%) |
| **long no-match search and render** — 474.5 ms | 510.5 ms (+7.6%) |
| **long near-match search and render** — 483.8 ms | 453.5 ms (-6.3%) |

Compared complete painted runs against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
