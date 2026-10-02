# Playlist collection

## 100k unique track IDs

| Baseline | Optimized |
| --- | --- |
| Publish first — 831.2 ms | 163.9 ms (-80.3%) |
| Publish repeat — 739.5 ms | 78.1 ms (-89.4%) |
| Publish allocation — 167.9 MiB | 18.8 MiB (-88.8%) |
| Clear repeat — 0.22 ms | 8.31 ms (+3707.2%) |

## 100k occurrences / 50k track IDs

| Baseline | Optimized |
| --- | --- |
| Publish first — 834.1 ms | 179.1 ms (-78.5%) |
| Publish repeat — 814.5 ms | 85.1 ms (-89.6%) |
| Publish allocation — 167.9 MiB | 18.8 MiB (-88.8%) |
| Clear repeat — 0.18 ms | 6.94 ms (+3710.6%) |

Collection publication with real saved song views, synchronous membership observers and WPF ListCollectionView. Database reads, view construction, dispatcher scheduling, disposal and painting are excluded. Repeat values use the post-first samples; raw JSON retains every sample.

Clear includes subscription cleanup, which the baseline omitted. Removed/cleared rows no longer report updates; replacement views stay current, duplicate references retain one update per remaining occurrence, and moves preserve membership.

Native application results are reported separately. These component timings do not establish cold-start or full UI gains.

Source: 04752a8f / 7e64f2dc → b65ef9ce.
