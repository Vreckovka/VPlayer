# Playlist: incoming views

Visible workspace builds, copied 207k-item library and 100k-entry playlists. Unique and duplicate workloads use their own frozen baselines. Timings include the first sample and slowest sample; these are fresh processes, not restart-based cold measurements.

| Baseline now | New optimized version |
| --- | --- |
| **Unique / first playlist visible** — 12.3k ms | 13.5k ms (+9.6%) |
| **Unique / slowest playlist visible** — 12.3k ms | 13.5k ms (+9.6%) |
| **Unique / slowest incoming read + views** — 4.1k ms | 3.5k ms (-13.7%) |
| **Unique / slowest database read** — 2.7k ms | 3.4k ms (+26.6%) |
| **Unique / slowest view creation** — 1.4k ms | 75.5 ms (-94.5%) |
| **Unique / component first read + views** — 4.6k ms | 3.4k ms (-26.5%) |
| **Duplicates / first playlist visible** — 10.6k ms | 14.2k ms (+33.7%) |
| **Duplicates / slowest playlist visible** — 12.2k ms | 14.2k ms (+17%) |
| **Duplicates / slowest incoming read + views** — 3.9k ms | 3.2k ms (-18%) |
| **Duplicates / slowest database read** — 2.3k ms | 3.1k ms (+35%) |
| **Duplicates / slowest view creation** — 1.6k ms | 68.8 ms (-95.6%) |
| **Duplicates / component first read + views** — 4.2k ms | 3.4k ms (-18.7%) |
| **Component / slowest creation allocation** — 552 MiB | 18.5 MiB (-96.6%) |
| **Unique / slowest paired playlist visible** — 16k ms | 11.7k ms (-26.7%) |
| **Duplicates / slowest paired playlist visible** — 14.2k ms | 11.6k ms (-18.3%) |

The default factory resolves shared singleton services once per enumeration and still creates an independent view for every occurrence. Custom factories, explicit view activation and contextual/transient bindings retain their original creation path. A regression test also exposed and fixed bypassed custom factory dispatch in saved playlist creation. All 241 unit/regression tests passed.

View construction improved, but the original series had slower first/worst full visible loads. The later paired runs alternate baseline/optimized, then optimized/baseline for each fixture; their slowest loads are shown separately. The original samples remain unchanged. The database query is unchanged; the read experiments were rejected because their first reads were slower. Positive percentages remain in the table. Database reading and later loading/publication stages remain the larger delays; the paired results do not establish a restart-based cold improvement.

Raw evidence: [component unique baseline](iterations/incoming-views-baseline-unique-f0256e6d.json), [component unique optimized](iterations/incoming-views-optimized-unique-987dd930.json), [component duplicate baseline](iterations/incoming-views-baseline-repeated-f0256e6d.json), [component duplicate optimized](iterations/incoming-views-optimized-repeated-987dd930.json), [WPF unique baseline](iterations/incoming-native-baseline-unique-9467632d.json), [WPF unique optimized](iterations/incoming-native-optimized-unique-987dd930.json), [WPF duplicate baseline](iterations/incoming-native-baseline-repeated-9467632d.json), [WPF duplicate optimized](iterations/incoming-native-optimized-repeated-987dd930.json), [validation](iterations/incoming-views-validation-987dd930.json).

Optimized source: `987dd930450f27d02e8cd18371cfae04b0322ed7`. Component baseline source: `f0256e6d3b870c6ea2b473933f624a52132ba020`; WPF baseline source: `9467632d367744c7511a5aebad3d2b12d62c4d69`. The running user app and installed files were untouched. This change has not been deployed.

Paired raw samples: [unique baseline](iterations/incoming-native-control-unique-baseline-987dd930.json), [unique optimized](iterations/incoming-native-control-unique-optimized-987dd930.json), [duplicates baseline](iterations/incoming-native-control-repeated-baseline-987dd930.json), [duplicates optimized](iterations/incoming-native-control-repeated-optimized-987dd930.json).
