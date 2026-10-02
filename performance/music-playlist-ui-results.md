# 100k-entry music playlist

Real WPF window; copied 207k-item library. Buffered diagnostics. - % = less time; + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **load and render** — Timeout (60k ms) | Pending |
| **collection publication** — Unfinished | Pending |
| **incoming song view conversion** — 5.6k ms | Pending |
| **saved playlist view creation** — 4.9k ms | Pending |
| **database and incoming views** — 3k ms | Pending |
| **activation and render** — 1.6k ms | Pending |
| **scroll to last track** — Not reached | Pending |
| **long no-match search and render** — Not reached | Pending |
| **long near-match search and render** — Not reached | Pending |

The timeout is the overall process limit. Unfinished endpoints have no percentage; completed phase medians exclude the small startup playlist. Raw phases, failures and provenance stay in JSON.
