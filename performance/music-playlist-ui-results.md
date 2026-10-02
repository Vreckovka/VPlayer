# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 662.7 ms | 583 ms (-12%) |
| **load and render** — 32.4k ms | 15.9k ms (-51%) |
| **stored song enrichment** — 2.3k ms | 4.7k ms (+108.1%) |
| **collection publication** — 15.2k ms | 4.7k ms (-69.4%) |
| **collection replacement** — 11.3k ms | 4.4k ms (-60.9%) |
| **incoming song view conversion** — 4.8k ms | 3.41 ms (-99.9%) |
| **saved playlist view creation** — 4.6k ms | 130.6 ms (-97.2%) |
| **active item dispatch** — 3.8k ms | 217.8 ms (-94.2%) |
| **database and incoming views** — 3.2k ms | 3.5k ms (+10.9%) |
| **stored song read** — 2k ms | 2.5k ms (+22.7%) |
| **activation and render** — 2.8k ms | 2.2k ms (-19.5%) |
| **scroll to last track** — 222.3 ms | 687.2 ms (+209.2%) |
| **long no-match search and render** — 474.5 ms | 972.8 ms (+105%) |
| **long near-match search and render** — 483.8 ms | 827.6 ms (+71.1%) |

Completed endpoint timings compare against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.
