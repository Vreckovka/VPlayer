# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 662.7 ms | 552.3 ms (-16.7%) |
| **load and render** — 32.4k ms | 24.8k ms (-23.3%) |
| **stored song enrichment** — 2.3k ms | 2.7k ms (+20.8%) |
| **collection publication** — 15.2k ms | 9.1k ms (-40.2%) |
| **collection replacement** — 11.3k ms | 4.7k ms (-58.7%) |
| **incoming song view conversion** — 4.8k ms | 3.32 ms (-99.9%) |
| **saved playlist view creation** — 4.6k ms | 5.3k ms (+14.1%) |
| **active item dispatch** — 3.8k ms | 4.5k ms (+19.2%) |
| **database and incoming views** — 3.2k ms | 3.5k ms (+8.8%) |
| **stored song read** — 2k ms | 2.3k ms (+11.4%) |
| **activation and render** — 2.8k ms | 1.1k ms (-59.1%) |
| **scroll to last track** — 222.3 ms | 458.5 ms (+106.3%) |
| **long no-match search and render** — 474.5 ms | 572.6 ms (+20.7%) |
| **long near-match search and render** — 483.8 ms | 796.2 ms (+64.6%) |

Compared complete painted runs against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
