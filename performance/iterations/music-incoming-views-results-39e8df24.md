# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 2.2k ms | 3k ms (+36.1%) |
| **load and render** — 27k ms | 23.1k ms (-14.2%) |
| **stored song enrichment** — 2.8k ms | 2.4k ms (-16.1%) |
| **collection publication** — 8.4k ms | 9.1k ms (+7.8%) |
| **collection replacement** — 4.4k ms | 5.2k ms (+18.5%) |
| **incoming song view conversion** — 5.1k ms | 3.81 ms (-99.9%) |
| **saved playlist view creation** — 5.3k ms | 5.9k ms (+10.7%) |
| **active item dispatch** — 3.9k ms | 4.2k ms (+8.2%) |
| **database and incoming views** — 3k ms | 3.5k ms (+15.3%) |
| **stored song read** — 2.1k ms | 2.1k ms (-1.4%) |
| **activation and render** — 1.8k ms | 1.7k ms (-4.9%) |
| **scroll to last track** — 323.8 ms | 191.5 ms (-40.9%) |
| **long no-match search and render** — 630.5 ms | 510.5 ms (-19%) |
| **long near-match search and render** — 530 ms | 453.5 ms (-14.4%) |

Compared complete painted runs against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
