# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 662.7 ms | 336.4 ms (-49.2%) |
| **load and render** — 32.4k ms | 16.6k ms (-48.6%) |
| **stored song enrichment** — 2.3k ms | 2.7k ms (+19.2%) |
| **collection publication** — 15.2k ms | 4.1k ms (-72.8%) |
| **collection replacement** — 11.3k ms | 3.9k ms (-65.3%) |
| **incoming song view conversion** — 4.8k ms | 3.1 ms (-99.9%) |
| **saved playlist view creation** — 4.6k ms | 4.3k ms (-7.3%) |
| **active item dispatch** — 3.8k ms | 170.2 ms (-95.5%) |
| **database and incoming views** — 3.2k ms | 3.2k ms (+0.8%) |
| **stored song read** — 2k ms | 2k ms (-1.6%) |
| **activation and render** — 2.8k ms | 1.6k ms (-40.6%) |
| **scroll to last track** — 222.3 ms | 379.5 ms (+70.7%) |
| **long no-match search and render** — 474.5 ms | 699.3 ms (+47.4%) |
| **long near-match search and render** — 483.8 ms | 847 ms (+75.1%) |

Completed endpoint timings compare against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.

Load timeout retained (60k ms limit); medians use completed endpoints, and consistent loading remains unproven.
