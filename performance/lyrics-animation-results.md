# Lyrics animation and UI work

Observed component results: 100k lyric lines, then 1k lyrics track/context changes, with synthetic 16ms playback ticks. Visible isolated WPF harness, production timeline and scroll behavior, simplified rows. Audio decoding and the full player UI are not included. Rendering gaps are distinct WPF rendering opportunities, not measured display FPS.

| Baseline now | New optimized version |
| --- | --- |
| **After 1k changes / process CPU** — 450.8 ms/s | 153.8 ms/s (-65.9%) |
| **After 1k changes / UI allocations** — 34.5 MiB/s | 1.5 MiB/s (-95.8%) |
| **Fresh + changed tracks / slowest rendering gap** — 164.6 ms | 39.4 ms (-76.1%) |
| **Fresh + changed tracks / slowest dispatcher delay** — 148.6 ms | 26.9 ms (-81.9%) |
| **Fresh lyrics / process CPU** — 417.3 ms/s | 234.6 ms/s (-43.8%) |
| **1k lyrics context changes** — 238.3 ms | 293.8 ms (+23.3%) |

Old lyrics previously remained observed and could scroll the new track. The fix disposes replaced listeners, checks callback ownership, and cancels superseded animations on seeks, context changes and unloading. Current-line highlighting and final offsets were verified visually and numerically. Five new regressions failed before the fix; all 246 tests now pass.

The first frozen comparison uses the final v3 harness. Earlier v1/v2 drafts remain diagnostic timing evidence only; their frame/row setup differs, and v1 screenshot paths were reused by v2. They are excluded from this table and visual validation.

Raw [baseline](iterations/lyrics-animation-baseline-5cdf6243.json), [optimized](iterations/lyrics-animation-optimized-24d31ad1.json), and [validation](iterations/lyrics-animation-validation-24d31ad1.json). Source: `24d31ad1`. Running user app and installed files untouched; no deployment. Full-player/audio playback and file-browser search measurements remain next.

Automatic playback refresh now keeps the loaded timeline, active line and adjustments, and can load stored synchronized lyrics before album/artist metadata is available. Cached plain text remains visible while optional synchronized lyrics load. The manual refresh command still requests a provider update. Seven stored-lyrics regressions and all 32 lyrics checks passed. This is correctness evidence; full-player audio playback remains unmeasured for this change.
