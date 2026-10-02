# Saved playlist view creation

100k stored entries; real Ninject with stub services. Component timings exclude database reads, rendering, verification and disposal.

| Baseline now | New optimized version |
| --- | --- |
| **first batch** — 3.5k ms | 112 ms (-96.8%) |
| **repeated batch** — 3.4k ms | 120.8 ms (-96.4%) |
| **allocated per batch** — 1.8 GiB | 48.2 MiB (-97.4%) |

Source commits: `978a4d33` → `269120f3`. Original samples and validation remain in the raw JSON.
