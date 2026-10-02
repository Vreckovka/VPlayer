# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 662.7 ms | 2.2k ms (+228%) |
| **load and render** — 32.4k ms | 27k ms (-16.7%) |
| **stored song enrichment** — 2.3k ms | 2.8k ms (+25%) |
| **collection publication** — 15.2k ms | 8.4k ms (-44.7%) |
| **collection replacement** — 11.3k ms | 4.4k ms (-61.2%) |
| **incoming song view conversion** — 4.8k ms | 5.1k ms (+5.3%) |
| **saved playlist view creation** — 4.6k ms | 5.3k ms (+14%) |
| **active item dispatch** — 3.8k ms | 3.9k ms (+2.3%) |
| **database and incoming views** — 3.2k ms | 3k ms (-5%) |
| **stored song read** — 2k ms | 2.1k ms (+4.8%) |
| **activation and render** — 2.8k ms | 1.8k ms (-35.5%) |
| **scroll to last track** — 222.3 ms | 323.8 ms (+45.7%) |
| **long no-match search and render** — 474.5 ms | 630.5 ms (+32.9%) |
| **long near-match search and render** — 483.8 ms | 530 ms (+9.6%) |

Compared complete painted runs using the prior version as baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.

Read timeout retained in raw evidence; medians use completed runs.
