# Statistics

207k sound items with unique file metadata; 5,310 playlists.
First data load and repeat medians; - % = less time, + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **first data load** — 4.4k ms | 1.9k ms (-55.9%) |
| **warm data load** — 2.9k ms | 299.4 ms (-89.5%) |
| **load and render** — 6.2k ms | 2.6k ms (-57.3%) |


Data timing includes production loading and UI publication. View timing includes navigation and actual WPF rendering. Full samples, allocations, row checks and provenance remain in the Statistics JSON files.
