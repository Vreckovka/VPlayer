# Performance baselines

The primary workload is a SQLite backup of the current library, expanded tenfold, with
1,000-, 10,000-, and 100,000-entry playlists. The backup API includes committed WAL data.
Fixture databases, titles, file paths, settings, and raw logs stay under ignored artifacts/.
The fixture checksum must match on every run. Preparation refuses to overwrite an existing copy.

Prepare once:

    dotnet run --project tests/VPlayer.Performance -p:Platform=x64 -- prepare "<source.db>" artifacts/performance/library-worst-case 10

Measure before changing an implementation:

    pwsh -File performance/run.ps1 -Mode baseline -Visible

Measure the same fixture after a verified change:

    pwsh -File performance/run.ps1 -Mode optimized -Visible

Use Release x64, the same machine/runtime/power settings, and no concurrent builds or tests.
Do not compare runs with different fixture checksums, workloads, build configurations, or runtimes.
The first sample is recorded separately; later samples report median and p95.
Fresh application processes are measured through the first main-window ContentRendered event.
OS disk caches are not cleared: these are fresh-process measurements, not cold-disk measurements.
Use the same window mode for both runs. Visible launches are authorized for this project; other users should explicitly authorize them before an agent opens windows.
First render does not mean all asynchronous libraries or services are ready. A separate initial-playlist-view milestone waits for the production collection to load, its loading status to clear, and the visible playlist ListView to generate a nonempty row. This prevents an empty view from being reported as ready. Screenshots of both milestones stay local.

Startup uses a separate writable copy per launch through VPLAYER_BENCHMARK_DIRECTORY,
with settings redirected into that copy. Ordinary launches use the existing paths.
A startup failure or timeout is recorded as a failure, never as a faster timing.
Startup scopes overlap; their durations must not be summed.

## Measurement categories

- Application: first window render, initialization, module registration, container activation,
  settings, shell construction. Record native/service failures separately.
- Data: sound items and file metadata, artists, albums and artist relationships.
- Playlists: summaries and complete production GetItemsToPlay enumeration at three stress sizes.
  The production Ninject factory is included; playback and network providers are outside this boundary.
- UI: view-model construction, worst-size title sorting/filtering, virtualized list layout and
  scroll jumps. Offscreen text-row ListBox tests are component scenarios; app card templates,
  navigation, details, file browser, settings, video/fullscreen, and modal flows need separate
  end-to-end scenarios before claiming those features are optimized.
- Lyrics: 10,000 seeks over 1,000, 10,000, and 100,000 timed lines; include untimed/duplicate
  timestamps, backward seeks, and persistence errors in regression tests.
- Spectrum: 1,000 stable frames and 1,000 rapidly changing frames. Stable-frame improvements
  must not be presented as the cost of continuously changing audio.
- Media discovery and metadata: deep trees, mixed extensions, inaccessible/reparse folders,
  blank tags, unavailable fingerprints, and slow/unavailable providers.

## Prioritization

Within each category, optimize the largest measured wall-time or UI stall first.
Overall, prioritize the largest user-visible critical-path delays, then repeated frame/tick work.
Convert batched workloads to per-operation cost before comparing them; do not compare
1,000 frames directly with a single startup. Use frequency to judge repeated costs.
Retest every metric in the affected category and run the regression suite.
Keep baseline samples immutable. Record the optimized commit and any regressions.
Only aggregate timings and non-sensitive workload counts belong in committed reports.

Diagnostic query/factory breakdown: run the Release performance executable with profile-playlist <fixture-directory> unused. These single samples identify a candidate bottleneck; they do not replace the repeated comparison protocol.

The populated initial-view startup baseline is stored separately in startup-ready-baseline.json at commit f9bdaa8a. The original seven visible baseline launches crashed before rendering, so no populated-view time existed there. report.ps1 accepts StartupOptimized for a startup-only comparison and ReadyBaseline for this additional frozen milestone.

Startup phase baselines are retained in startup-detail-baseline.json and startup-shell-baseline.json. The report uses the first recorded baseline containing each phase, names those commits, and keeps the original failed startup baseline. Library query setup moves to the loading worker; artist/album/TV relationships and playlist privacy/order are verified against real SQLite queries.

Query deferral is evaluated against the frozen startup-shell-baseline.json as well as the earlier recovered-startup milestone. Constructor work and repository setup are reported separately; moving work to a background thread is not treated as eliminating its cost. Cold-first launches and maxima remain visible.

Measured query-deferral result (94ed1e61 to cc489285): music-playlist view-model construction median 534.46 to 11.70 ms; main-window view-model initialization 998.39 to 538.28 ms; first window render 3528.59 to 3302.87 ms. Populated-view median remained effectively unchanged at 5108.71 versus 5108.83 ms. This reduces foreground construction work; it does not establish a populated-view speedup. Seven successful visible launches per version; maxima are retained in the report.

The actual grouped playlist UI has its own isolated fixture and report, grouped-ui-results.md.
Create it with the Release performance executable:
prepare-ui <expanded-library-fixture> <new-fixture-directory> 5000.
This retains the 207,110 sound items and 1k/10k/100k stress playlists, then adds 5,000
favorites with long titles. The original fixture is checksum verified and never modified.
Run startup.ps1 with -Visible -ScrollPlaylists against this fixture for seven fresh-profile
launches per version. The ready milestone still requires a rendered row; scrolling must
bring the last favorite fully into its scroll viewport. Local screenshots and realized-row
counts validate the real GridView template, rather than the simpler component ListBox.
Use grouped-ui-report.ps1 -Baseline <baseline-directory-or-json> -Optimized <optimized-directory-or-json>.
This report rejects mismatched fixtures, runtime, configuration, machine or window mode.
Timeouts remain failed workloads. Baselines for the smaller initial view are retained separately.
Generated-playlist load-more expansion, filtering and group changes still need their own scenarios.

Measured grouped-UI result (64a18e1f to c2558db9): the seven baseline launches all
timed out at 60 seconds before the populated view. All seven optimized launches
rendered the view and reached the last favorite: ready median 5,576.12 ms, maximum
7,895.76 ms; scroll median 280.00 ms, maximum 435.53 ms. Only 41 row containers
were realized for 5,073 displayed playlists, both before and after scrolling.
The existing columns, commands and header layout are retained. Selection across
groups, deselection, unloading/reloading and global Home/End and boundary Up/Down
navigation are covered by six regressions over 10,000 items; the full suite passes 100 tests.

The smaller expanded-library startup scenario also completed seven times at c2558db9:
populated-view median 4,700.49 ms, versus 5,108.83 ms in the previous cc489285 run.
The latest full-library data-read control measured 3,807.53 ms, versus 2,640.81 ms
in the original feature baseline. That query code did not change in this UI task;
the report preserves the slower observation and does not credit it to an optimization.
Prior cc489285 feature, startup and report records are retained in iterations/.
The complete application goal remains active: prioritize the largest measured data
load next, and add separate worst-case baselines for the pending real UI features.

Report display uses compact median times (for example 5.1k ms) and signed percentage
changes calculated from unrounded values: negative means less time, positive means
more time. Startup breakdowns are collapsed in results.md. Raw JSON retains launch
counts, first samples, maxima, percentiles, allocations, commits and fixture hashes.
A missing or failed timing baseline has no percentage.

Statistics uses a separate worst-case fixture made from the grouped-playlist copy.
prepare-statistics clones file metadata so all 207,110 sound items have independent
FileInfo rows; it retains the 5,310 playlists and 100k-entry playlist. The parent
and generated databases are checksum verified. No user library is modified.

The statistics command runs production StatisticsViewModel.LoadData, pumps UI
publication, and enumerates all four displayed lists. startup.ps1 -Visible -Statistics
measures actual navigation and populated WPF rendering with a fresh disposable
profile per launch. Statistics data and UI baselines are frozen separately in
statistics-baseline.json and statistics-ui-baseline.json. statistics-report.ps1
checks fixture, workload, runtime, configuration, machine and window compatibility,
uses only completed UI samples, flags incomplete series, rejects series with no
completed samples, and uses the same compact timing/percentage format.

The Statistics optimization streams Id/TimePlayed and Id/TotalPlayedTime scores,
keeps 30 candidates per media/playlist type, then fetches only bounded display
metadata. It preserves exact tick totals, private filtering and stable ties.
Song enrichment matches SoundItem type and ID together. Snapshot arrays preserve
the first row and remove null placeholders; Sounds/Videos bindings now match
their property names. Six regressions cover 10k unique files and playlists,
overlapping media IDs, empty/private libraries, long times, ties and reloads.
Two initial ranking regressions failed before the fix; the full suite now passes
106 tests. Production Release builds retain the repository's existing warnings.

At 36a1f5c3 the warm Statistics data median fell from 2.9k ms to 471 ms (-83.5%).
Managed allocation median fell from 460,041,488 to 105,761,112 bytes (-77.0%).
The first isolated data load increased from 4.4k ms to 6k ms (+38.4%); that regression
is explicitly reported and remains an optimization target. Completed visible-page
loads improved from 6.2k ms to 4k ms (-35.2%), with no null item/playlist rows.
One optimized launch timed out before Statistics while video player activation
took 54.1k ms. Its raw record is retained in statistics-ui-optimized.json; the UI
median is marked as an incomplete series. Screenshots of both versions were
visually checked. The immutable fixture checksum remained unchanged.
Prioritize that startup delay and first-load query/JIT costs in the continuing
application audit; the broader application goal is not yet complete.

Focused traces at 18875173 split native core loading, LibVLC construction,
MediaPlayer construction and event setup. All traced launches completed, with
LibVLC instance construction below 300 ms; the earlier 54.1k ms delay did not
recur, and its cause remains unproven. The trace is retained under iterations/.
No claim is made that instrumentation fixed it.

Statistics query traces identify sound-score reading as the largest query phase.
The first isolated traced load was 1.9k ms, showing substantial variation against
the earlier 6k ms sample. These instrumented timings are diagnostic records,
not replacements for the uninstrumented baseline/optimized comparisons.
The stress fixture has 143,610 zero-time sound rows out of 207,110 (69.3%).
Such rows do not contribute to totals; only 30 smallest public IDs are needed
for ranking. Negative legacy times and entirely unplayed 10k-item libraries
are covered explicitly before reducing score materialization.

Episode name, length and favorite setters previously assigned their existing
values instead of the supplied value. Three regressions reproduced the failures;
the corrected setters pass four tests exercising 10k episodes, long names,
64-bit lengths, toggling both ways and absent video references (9be2de8d).

The first zero-row optimization improved the copied library but slowed a fully
played workload by 9.4%; both records are retained under iterations/. The final
39d21c64 implementation samples the first 30 public IDs. It uses zero-row reduction
only when all are unplayed, reusing those zero candidates; otherwise it keeps the
full score scan. Parsed legacy zero strings are filtered to prevent duplicate IDs.
All 113 tests pass, including 10k zero/negative-time and legacy-format libraries.
Release application and benchmark builds succeeded with existing warnings.

Copied-library Statistics warm data is 299.4 ms (-89.5% from the original baseline),
with 44,911,648 managed bytes (-90.2%). The fully played fixture has 207k unique
positive play times and unique file metadata, so every sound must be totaled and
leaderboard insertions remain worst case. Its warm data is 514 ms versus 508 ms
(+1.2%) at 18875173; first data is 1.7k ms (+1.4%).
The added fixture and baseline belong to this later optimization stage, separately
from the original full-entity Statistics baseline.

Copied-library visible rendering is 2.6k ms (-57.3%) on median, but includes an
18.4k ms outlier. Fully played rendering was slower in the initial series; an
additional unchanged-code series confirmed substantial variation. Both complete
series are retained and combined, giving 4.2k ms versus 3.5k ms (+20.3%).
The report preserves the regression; it is not credited as an improvement.
Sound/video scores and small metadata fetches have multi-second outliers in the
real application despite much faster isolated runs, so diagnose concurrent work,
database waits and scheduling before attributing these delays to query logic.
Screenshots of both fixtures were visually checked and immutable hashes verified.
The earlier native initialization timeout remains unproven; the goal continues.

Reader traces at 29d1ce75 are retained in
iterations/statistics-query-waits-29d1ce75.json. Growing diagnostic snapshots
were synchronously written roughly 124 times per launch, costing 1.0–1.5k ms
in total, including individual writes above 500 ms. Reader lifetimes also
contained multi-second outliers, so file logging does not explain all delays.

Diagnostics now coalesce snapshots on a background writer and atomically replace
the last complete JSON file. Explicit final flushing occurs after timed UI work.
Three tests cover blocked disk writes with 10k concurrent producers, final
snapshot durability and disk failure propagation; all 116 tests pass.
New traces identify the protocol as buffered-v1. Report scripts reject mixed
protocol comparisons: changing the observer is not an application speedup.
Historical UI comparisons retain their original synchronous protocol; establish
a separate buffered baseline before further UI optimization.

The buffered-v1 fully played UI baseline at fed56a67 is frozen in
buffered-ui-baseline.json. All launches completed, with zero empty Statistics
rows; the last screenshot was visually verified. The source fixture hash is
unchanged. Diagnostic write counts fell to roughly 20–27, with background writes
still showing variable disk delays. Real page timings also remain variable.
buffered-ui-results.md keeps the requested compact two-column format.
ui-report.ps1 checks full commit hashes, fixture/machine/window consistency and
diagnostic protocol before comparing versions; same-series zero changes and
mixed-protocol rejection were verified. The optimized column stays pending until
an application change is measured against this new baseline.

Current-song relationship lookup now selects one album and one artist through
the existing configured EF queries, without opening their full libraries.
Repeated lookups reuse view models; later full loading reuses those instances
and refreshes complete model snapshots on the UI thread, preserving playback
and playlist flags. Cached entity and nested relationship notifications refresh
only affected entries; transient database read failures retain the last valid
snapshot and can retry. Cached reads need no dispatcher round trip.

Five regressions use 10k-row libraries or nested snapshots. They cover 64
concurrent lookups, bounded reader operations, missing/newly inserted rows,
configured query filtering, shared identity after full loading, clearing during
an in-flight load, UI-blocked synchronous lookup, metadata notifications and
read-failure recovery. The full-load compatibility path failed three initial
regressions before the bounded implementation. All 121 tests and the production
Release build pass. The buffered UI baseline remains frozen for comparison;
publish measured results only after the source commit is benchmarked.

At 0884d95f all fully played UI launches completed without full Album/Artist
library loads or empty Statistics rows. The application also resolves other
realized songs, so it legitimately performs additional bounded album lookups.
Both unchanged-build series are retained together in buffered-ui-optimized.json;
the original buffered baseline remains immutable at fed56a67.
Combined Statistics render median is 4.6k ms versus 5.8k ms (-21.4%).
Initial playlist readiness is 4.7k ms versus 4.9k ms (-4.7%); first render is
effectively unchanged (+0.2%). This is an observed median comparison, with
substantial timing variation. Two initial Statistics outliers include a 14.8k ms
run, worse than the baseline maximum; score-reader waits dominate that run.
Do not claim that all render delays were fixed or discard the slow samples.

The final screenshot was visually verified, the source fixture checksum stayed
unchanged, and CodeGraph sync found the index current. Main results.md now shows
the current buffered startup/Statistics comparison, with older comparisons
folded under details. The broad application audit and optimization goal continues.
