# Rapid playlist selection

100k entries; controlled media preparation with a mocked device. These are correctness checks, not playback-speed measurements.

| Baseline now | New optimized version |
| --- | --- |
| **100 out-of-order skips: stale media applies** — 99 | 0 (-100%) |
| **Clear during preparation: unhandled exceptions** — 1 | 0 (-100%) |
| **Same track in another occurrence: playback starts** — 2 | 1 (-50%) |
| **Previous from first: selected index** — 100k (invalid) | 99999 (last item) |

Twelve checks also cover repeated requests, end of queue, device replacement, disposal, delayed initialization, media clearing with the same selection, and normal playback. Device timing, database-save cost and remote-media cancellation remain to measure.

[Baseline](playback-selection-baseline.json); [optimized ebff741c](playback-selection-optimized.json); [test/build evidence](iterations/playback-selection-tests-ebff741c.txt).
