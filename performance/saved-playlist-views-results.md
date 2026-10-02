# Saved playlist view creation

100k stored entries; real Ninject, eight shared stub services and a separate WindowManager per entry. Component timings exclude database reads, rendering, verification and disposal.

| Baseline now | New optimized version |
| --- | --- |
| **first batch** — 3.8k ms | 157.1 ms (-95.8%) |
| **repeated batch** — 3.9k ms | 217.9 ms (-94.4%) |
| **allocated per batch** — 1.9 GiB | 57.4 MiB (-97.1%) |

Source commits: `5074f377` → `171b2af1`. Original samples and validation remain in the raw JSON.
