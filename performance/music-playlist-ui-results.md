# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **clear before load** — Unfinished | 662.7 ms |
| **load and render** — Timeout (60k ms limit) | 32.4k ms |
| **stored song enrichment** — Unfinished | 2.3k ms |
| **collection publication** — Unfinished | 15.2k ms |
| **collection replacement** — 10.9k ms | 11.3k ms (+3.8%) |
| **incoming song view conversion** — 5.6k ms | 4.8k ms (-13.6%) |
| **saved playlist view creation** — 4.9k ms | 4.6k ms (-5.8%) |
| **active item dispatch** — 4.1k ms | 3.8k ms (-7.6%) |
| **database and incoming views** — 3k ms | 3.2k ms (+5%) |
| **stored song read** — 2.1k ms | 2k ms (-1.7%) |
| **activation and render** — Not measured (painted) | 2.8k ms |
| **scroll to last track** — Not reached | 222.3 ms |
| **long no-match search and render** — Not reached | 474.5 ms |
| **long near-match search and render** — Not reached | 483.8 ms |

The timeout is the overall process limit. Unfinished endpoints have no percentage; completed phase medians exclude the small startup playlist. Clear uses the separate save-wait control; stored-song/replacement/dispatch rows use the pre-lookup control. Painted activation has no prior baseline. Raw phases, failures and provenance stay in JSON.
