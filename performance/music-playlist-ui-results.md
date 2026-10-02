# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **load and render** — Timeout (60k ms limit) | Pending |
| **stored song enrichment** — Unfinished | Pending |
| **collection publication** — Unfinished | Pending |
| **collection replacement** — 10.9k ms | Pending |
| **incoming song view conversion** — 5.6k ms | Pending |
| **saved playlist view creation** — 4.9k ms | Pending |
| **active item dispatch** — 4.1k ms | Pending |
| **database and incoming views** — 3k ms | Pending |
| **stored song read** — 2.1k ms | Pending |
| **activation and render** — Not measured (painted) | Pending |
| **scroll to last track** — Not reached | Pending |
| **long no-match search and render** — Not reached | Pending |
| **long near-match search and render** — Not reached | Pending |

The timeout is the overall process limit. Unfinished endpoints have no percentage; completed phase medians exclude the small startup playlist. Stored-song/replacement/dispatch rows use the separate pre-lookup control. Painted activation has no prior baseline. Raw phases, failures and provenance stay in JSON.
