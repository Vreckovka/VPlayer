# Statistics

207k sound items with unique file metadata; 5,310 playlists. First data load and repeat medians; - % = less time, + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **first data load** — 4.4k ms | 6k ms (+38.4%) |
| **warm data load** — 2.9k ms | 471 ms (-83.5%) |
| **load and render** — 6.2k ms | 4k ms (-35.2%)* |

* UI timing uses completed Statistics samples; raw JSON retains the incomplete launch.

Data timing includes production loading and UI publication. View timing includes navigation and actual WPF rendering. Full samples, allocations, row checks and provenance remain in the Statistics JSON files.
