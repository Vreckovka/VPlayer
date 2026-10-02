# Grouped playlist UI

207k sound items; 5,310 playlists including 5k added favorites. Median timings; - % = less time, + % = more time.

| Baseline now | New optimized version |
| --- | --- |
| Populated view: Timeout (60k ms) | 5.6k ms |
| First frame*: 3.6k ms | 3.2k ms (-10.8%) |
| Scroll to last favorite: n/a | 280 ms |
| Realized rows (ready / scrolled): n/a / n/a | 41 / 41 |

*The baseline rendered its first frame, then timed out before becoming usable. Timeout comparisons have no percentage. Full samples and provenance are retained in grouped-ui-baseline.json and grouped-ui-optimized.json.
