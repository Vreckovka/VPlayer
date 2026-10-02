# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 336.4 ms | 583 ms (+73.3%) |
| **load and render** — 16.6k ms | 15.9k ms (-4.7%) |
| **stored song enrichment** — 2.7k ms | 4.7k ms (+74.6%) |
| **collection publication** — 4.1k ms | 4.7k ms (+12.7%) |
| **collection replacement** — 3.9k ms | 4.4k ms (+12.4%) |
| **incoming song view conversion** — 3.1 ms | 3.41 ms (+10%) |
| **saved playlist view creation** — 4.3k ms | 130.6 ms (-97%) |
| **active item dispatch** — 170.2 ms | 217.8 ms (+27.9%) |
| **database and incoming views** — 3.2k ms | 3.5k ms (+10%) |
| **stored song read** — 2k ms | 2.5k ms (+24.7%) |
| **activation and render** — 1.6k ms | 2.2k ms (+35.4%) |
| **scroll to last track** — 379.5 ms | 687.2 ms (+81.1%) |
| **long no-match search and render** — 699.3 ms | 972.8 ms (+39.1%) |
| **long near-match search and render** — 847 ms | 827.6 ms (-2.3%) |

Completed endpoint timings compare against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.

Baseline load timeout retained (60k ms limit); timings use completed endpoints.
