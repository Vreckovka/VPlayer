# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 436.5 ms | 336.4 ms (-22.9%) |
| **load and render** — 19.8k ms | 16.6k ms (-16%) |
| **stored song enrichment** — 3.3k ms | 2.7k ms (-16.7%) |
| **collection publication** — 4.8k ms | 4.1k ms (-13.7%) |
| **collection replacement** — 4.5k ms | 3.9k ms (-13.6%) |
| **incoming song view conversion** — 3.16 ms | 3.1 ms (-1.9%) |
| **saved playlist view creation** — 5k ms | 4.3k ms (-14.5%) |
| **active item dispatch** — 201.2 ms | 170.2 ms (-15.4%) |
| **database and incoming views** — 3.5k ms | 3.2k ms (-8.6%) |
| **stored song read** — 2.4k ms | 2k ms (-16.6%) |
| **activation and render** — 1.7k ms | 1.6k ms (-3.8%) |
| **scroll to last track** — 369.4 ms | 379.5 ms (+2.7%) |
| **long no-match search and render** — 701.7 ms | 699.3 ms (-0.3%) |
| **long near-match search and render** — 690.8 ms | 847 ms (+22.6%) |

Completed endpoint timings compare against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.

Load timeout retained (60k ms limit); medians use completed endpoints, and consistent loading remains unproven.
