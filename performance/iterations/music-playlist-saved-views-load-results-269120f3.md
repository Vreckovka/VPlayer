# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — 336.4 ms | 461.7 ms (+37.3%) |
| **load and render** — 16.6k ms | 16.7k ms (+0.2%) |
| **stored song enrichment** — 2.7k ms | 2.8k ms (+2.8%) |
| **collection publication** — 4.1k ms | 3.9k ms (-4.6%) |
| **collection replacement** — 3.9k ms | 3.7k ms (-5.7%) |
| **incoming song view conversion** — 3.1 ms | 3.16 ms (+2.1%) |
| **saved playlist view creation** — 4.3k ms | 4.4k ms (+1.4%) |
| **active item dispatch** — 170.2 ms | 179.5 ms (+5.5%) |
| **database and incoming views** — 3.2k ms | 3k ms (-5.6%) |
| **stored song read** — 2k ms | 2k ms (+1%) |
| **activation and render** — 1.6k ms | 1.5k ms (-9%) |
| **scroll to last track** — 379.5 ms | 392.4 ms (+3.4%) |
| **long no-match search and render** — 699.3 ms | 731.9 ms (+4.7%) |
| **long near-match search and render** — 847 ms | 686.3 ms (-19%) |

Completed endpoint timings compare against the frozen baseline. - % = less time; + % = more time. Raw phases and provenance stay in JSON.

Baseline load timeout retained (60k ms limit); timings use completed endpoints.
