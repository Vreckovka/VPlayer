# Playlist snapshot

100k stored entries. Component timings; database work, UI rendering and media playback are excluded.

| Baseline now | New optimized version |
| --- | --- |
| **first snapshot** — 3.9k ms | 186.7 ms (-95.2%) |
| **repeated snapshot** — 4k ms | 138.4 ms (-96.5%) |
| **allocated per snapshot** — 696.3 MiB | 30.7 MiB (-95.6%) |

Source commits: `5b1d2ac3` → `2abd4a10`. Original samples and validation remain in the raw JSON.
