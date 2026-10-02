# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 662.7 ms | 1.8k ms (+164.8%) |
| **load and render** — 32.4k ms | 26.4k ms (-18.6%) |
| **stored song enrichment** — 2.3k ms | 3.9k ms (+71.2%) |
| **collection publication** — 15.2k ms | 9.5k ms (-37.2%) |
| **collection replacement** — 11.3k ms | 4.7k ms (-58.2%) |
| **incoming song view conversion** — 4.8k ms | 3.74 ms (-99.9%) |
| **saved playlist view creation** — 4.6k ms | 5.7k ms (+23.4%) |
| **active item dispatch** — 3.8k ms | 4.4k ms (+16.6%) |
| **database and incoming views** — 3.2k ms | 3.1k ms (-1.6%) |
| **stored song read** — 2k ms | 3k ms (+44.6%) |
| **activation and render** — 2.8k ms | 1.6k ms (-41.4%) |
| **scroll to last track** — 222.3 ms | 570.8 ms (+156.8%) |
| **long no-match search and render** — 474.5 ms | 743.7 ms (+56.7%) |
| **long near-match search and render** — 483.8 ms | 767.4 ms (+58.6%) |

Compared complete painted runs against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
