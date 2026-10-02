# Statistics — fully played library

207k played sound items with unique metadata and play times; 5,310 playlists.
First data load and repeat medians; - % = less time, + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| **first data load** — 1.7k ms | 1.7k ms (+1.4%) |
| **warm data load** — 507.9 ms | 514 ms (+1.2%) |
| **load and render** — 3.5k ms | 4.2k ms (+20.3%) |


Data timing includes production loading and UI publication. View timing includes navigation and actual WPF rendering. Full samples, allocations, row checks and provenance remain in the Statistics JSON files.
