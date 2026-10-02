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

startup.ps1 -CpuProfile adds opt-in per-scope execution diagnostics:
thread CPU time, GC collection deltas and thread-pool counts at scope boundaries.
Windows CPU time comes from user/kernel GetThreadTimes values
([API documentation](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-getthreadtimes)).
Scopes that move between managed threads report unavailable CPU deltas.
GC counts are process-wide; thread-pool samples describe only the boundaries,
so neither directly proves the cause of a wait. Ordinary benchmarks leave this
profiling disabled; report compatibility checks reject mixed profiles.

Two regressions verify native CPU accounting during actual work and rejection
of a CPU delta across threads. All 123 tests and the production Release build
pass. These new profiles investigate the long score-reader delays and do not
replace the existing baseline/optimized comparisons.

The opt-in profiles at 63dba53c are retained in
[iterations/statistics-cpu-63dba53c.json](iterations/statistics-cpu-63dba53c.json).
Several long score reads used little thread CPU: a video score phase took
3.0k ms wall time with about 78 ms CPU; an episode score phase took 1.0k ms
with about 16 ms CPU and two generation-0 collections. Sound score CPU was
roughly 500–719 ms across the series. These observations separate computation
from waiting, but do not identify the wait's cause. Thread-pool queue counts
were zero at the sampled boundaries; intermediate queueing remains unmeasured.
CPU counter granularity also limits interpretation of short phases.
All launches rendered successfully. Profiling remains separate from the
baseline/optimized timing tables because its diagnostic profile differs.

The [covering-index experiment](iterations/statistics-index-probe-6a2089b5.md)
uses disposable copies of the fully played library. Production Statistics warm
loading improved by 3.2%, while first loading increased by 1.1%; indexes were
not adopted. Raw queries used covering scans. These data-load measurements
do not establish a fix for the longer UI waits. Valid paired samples remain in
the JSON, separate from application baseline/optimized results.

The cpu-v2 profile separates database connection opening and reader-command
execution from cursor enumeration. Phase DiagnosticEntryMilliseconds includes
the diagnostics lock and entry snapshot; DatabaseReads.DiagnosticWaitMilliseconds
records waiting to add a completed reader observation. Thread IDs correlate
concurrent operations. Database subscopes add records in memory without
requesting an extra snapshot per query. The ordinary buffered-v1/none protocol
keeps its existing records; profile compatibility checks reject cpu-v1/cpu-v2
and profiled/unprofiled comparisons.

Three operation-tracking regressions cover 10k operations across 64 workers,
duplicate IDs and failures. A native SQLite regression uses 10k long-name artists
and verifies connection/command completion for synchronous, asynchronous and
failed queries.
All 131 tests and the production Release build pass.

startup.ps1 -TraceTool <dotnet-trace.exe> optionally collects only the launched
benchmark PID. The collector window stays hidden; output is retained beside
startup JSON. Samples with an external trace receive a separate diagnostic
profile suffix and are not valid baseline comparisons. Sampled thread stacks
include waiting threads, so stack percentages are not CPU percentages
([tool documentation](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace)).
The local investigation uses Microsoft dotnet-trace 9.0.661903 in the ignored
artifacts/tools directory.

The cpu-v2 series at 711f8d97 is retained in
[iterations/statistics-execution-711f8d97.json](iterations/statistics-execution-711f8d97.json).
All views rendered; the final screenshot was verified and the fixture stayed
unchanged. Scope-entry overhead peaked around 25 ms; reader-record lock waits
stayed below 10 ms. Long phases still used little CPU, with delays in connection
opening, command execution or cursor lifetime, depending on the sample.

The process-specific
[EventPipe investigation](iterations/statistics-eventpipe-a79c1d17.json)
identified PlaylistViewModel.GetDisplayName background filesystem probes,
including Directory.Exists. Thread-stack percentages include waiting time.
This is evidence of unnecessary work, not proof that it caused every reader
delay. The unprofiled, fully played display-name baseline is frozen separately
in display-names-ui-baseline.json at a79c1d17 before changing the application.

Playlist display names now use synchronous string/path parsing. They no longer
start a task or probe Directory.Exists/File.Exists for each playlist. Plain and
relative custom titles stay verbatim; absolute paths show their last component,
including offline drives/shares, while drive roots stay intact. Empty names keep
the existing GENERATED/date fallback. Renames and fresh model snapshots now
refresh the cached display name.

Two 10k-row regressions reproduced asynchronous/stale labels before the fix;
ten path/title cases cover offline paths, roots, URLs and empty names. All 143
tests and the production Release build pass. The unprofiled UI baseline remains
frozen for the upcoming comparison.
The display-name task has two retained comparisons in
[display-names-ui-results.md](display-names-ui-results.md): the original seven
baseline/seven changed launches, followed by seven alternating launches per
version. The first control launch in the repeat series timed out, spending
about 43 seconds in VLC construction; six completed controls remain for its
median. It is retained rather than discarded. Statistics changed by +28.6%
in the first series and -6.7% in the repeat, so a consistent speedup from the
name change is unproven. The repeat first-render median also increased.

The repeat data and control-build provenance are in
[iterations/display-names-confirmation-5df42e33.json](iterations/display-names-confirmation-5df42e33.json).
The control was rebuilt from the previous playlist source; only that application
source differed between the compared commits. The changed source was restored
exactly and its Release build completed before alternating runs. The source
fixture checksum stayed unchanged; changed-app Statistics rows were populated
and the final rendered screenshot was inspected.

The main buffered table keeps its original immutable baseline and now uses
all fourteen unprofiled changed-app samples at 5df42e33, including slow outliers.
The earlier 0884d95f samples/report remain in iterations/buffered-ui-0884d95f.*.
Task comparisons use their own pre-change baseline and must not be interpreted
as equivalent to the earlier overall baseline. Raw JSON keeps individual launch
status, full precision, phases, observations and commit/fixture provenance;
the reader-facing tables keep compact times and signed percentages.

The statistics-reload component benchmark issues 32 overlapping production
LoadData requests against the fully played 207k-item/5,310-playlist fixture,
with independent read-only repositories and a real dispatcher. RepositoryCounts
records duplicate query groups. It includes UI publication and excludes XAML.
startup.ps1 -StatisticsReloads separately measures the same request burst after
Statistics activation in the actual WPF window, including layout and populated
rendered rows. Both baselines are captured before changing load coordination.

Statistics loads now share one in-flight task, including reentrant requests
raised by loading notifications. Both query groups finish before rows and
totals are published on the dispatcher; failed playlist queries retain the
previous snapshot. Task completion includes publication and loading-state
cleanup, and a subsequent request can retry after failure.

Four native SQLite/dispatcher regressions use 10k distinct sound records and
10k playlists. They reproduced duplicate/reentrant requests, completion before
publication and partial refresh on failure in the old implementation, and pass
with coordinated loading. All 147 tests pass; the production application and
performance runner Release builds complete with no errors. Frozen component
and rendered baselines remain separate from the upcoming optimized samples.

The coordinated-load comparison is retained in
[statistics-reload-results.md](statistics-reload-results.md). The real WPF
reload-burst median decreased from 8.4k ms to 1.2k ms (-85.7%); first component
bursts from 5.8k ms to 1.7k ms (-70.7%); repeated component bursts from 4.4k ms
to 514.5 ms (-88.3%). Each component burst opened seven repositories instead
of 224, across every sample. Component figures combine three fresh processes,
keeping first loads separate from their eight repeats; WPF series retain all
seven visible launches per version. Full precision and all samples stay in JSON.

The component baseline is at 35d65448, the WPF baseline at 01c32c3d, and the
optimized application at c7894170. Benchmark instrumentation was added before
changing the Statistics loading implementation. The fixture checksum remained
unchanged; every optimized WPF burst had populated rows, and the final reload
screenshot was inspected. No tests, builds or CPU tracing ran during timings.

The main startup/single-load table now compares its original buffered baseline
with the seven c7894170 launches. Those phases complete before each launch's
reload burst, so they keep their original timing boundaries. The previous
5df42e33 series/report remain in iterations/buffered-ui-5df42e33.*. The separate
playlist-name comparison and its mixed-direction results are still retained.

The startup preparation candidate shares the production public-playlist query
shape and prepares its async enumerator without advancing it. This moves model
setup and query compilation onto an early worker; it executes no row queries.
[EF query caching](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics)
is the relevant provider mechanism. Native EF 5.0.3/SQLite checks verify no
connection opening, reuse across contexts, 10k-row privacy/order correctness,
disposal after failure and successful retry. All 149 tests and the application
Release build pass.

The candidate is opt-in while measured: startup.ps1 -PreparePlaylistQuery
sets the application preparation flag and records PlaylistQueryPreparation in
each result. Ordinary launches keep preparation disabled until paired evidence
supports adoption. The baseline and prepared series alternate on the same
compiled application with buffered-v1/none diagnostics and fresh copied profiles.

The paired startup experiment is retained in
[playlist-preparation-results.md](playlist-preparation-results.md), with frozen
baseline/candidate JSON at f3f07535. Median query time fell from 1k ms to
159.7 ms (-84.6%), but view readiness improved only 3.9% and first render took
2.9% longer. Preparation remains disabled by default; this does not establish
an overall startup improvement worth adopting. Both modes had slow outliers,
which remain in the complete series. No builds, tests or tracing overlapped
these measurements. The source fixture checksum stayed unchanged, every launch
rendered the same populated playlist workload, and the final candidate view was
visually checked. All 149 tests and the Release build passed before the run.

playlist-preparation-report.ps1 validates preparation flags, same-commit
provenance, phase boundaries, untraced buffered diagnostics and rendered
workload consistency before generating the compact comparison.

The search component benchmark (`VPlayer.Performance search`) calls production
LibraryCollection.Filter and materializes both published result collections for
all copied titles. It excludes entity loading, view construction and XAML.
The no-match and one-character near-match queries use the longest fixture title,
forcing the existing fuzzy path to evaluate long queries across the full library.
Each fresh process retains its first sample and two repeats, including ordered
result hashes, allocations and full commit/fixture provenance. Baselines are
captured before replacing the edit-distance implementation.

The replacement matcher keeps the legacy invariant-case Levenshtein predicate
and its float threshold. It rejects impossible length differences, removes
common prefixes/suffixes without changing the original denominator, and visits
only the distance band needed for a match using two pooled rows. Library and
player predicates use it; player substring matching short-circuits before fuzzy
work while preserving its existing culture/contains behavior.

Six regression tests cover threshold boundaries, exhaustive short inputs,
random Unicode, long prefix/suffix cases, repeated long-input allocations,
10k-title result order and edits, and the actual player predicate under three
cultures. All 155 tests pass; application and performance runner Release builds
complete with no errors. The frozen pre-change search baseline is retained;
optimized results will be checked against its ordered result hashes.

The verified comparison is retained in
[fuzzy-search-results.md](fuzzy-search-results.md). Long queries across 207,110
copied titles fell from 12.4–13.3k ms to 39.5–42 ms (-99.7%). Warm allocation
medians fell from about 24.06 GB per query to about 106–110k bytes. The longest
fixture title is 114 characters. All ordered result hashes match: zero matches
for the no-match query and ten for the near-match query in every sample.

The frozen baseline uses 0a391935 and the optimized application f4fa1b16,
with three independent processes per version and first/repeated samples kept
separate. No tests or builds ran during timings. The fixture checksum stayed
unchanged. The report rejects changed boundaries, query text, result hashes,
counts, commits and fixture provenance. These figures cover production
filtering/publication; rendered search and full library loading remain separate
unfinished scenarios.

startup.ps1 -MusicPlaylist measures a 100k-entry playlist in the actual music
player: activation, database/view loading through the normal saved-playlist
event, populated rendering, scrolling to the last occurrence, and long no-match
and near-match searches through ActualSearch (including its debounce and WPF
publication). The benchmark verifies every loaded occurrence in order and
captures each rendered view. It uses a fresh disposable profile and separate
visible launches, with the existing overall process timeout. Small phase scopes
record incoming conversion, saved-row view creation and collection publication
without changing those paths. This establishes a new baseline before changing
large-playlist preparation.

The first native music-playlist pilot failed before the large load: clearing
the initial playlist left PlayVideo dereferencing ActualItem after an awaited
view initialization. The failure is retained in
iterations/music-playlist-clear-failure-9aede860.json; it is not a performance
baseline for the 100k load. Two dispatcher regressions reproduced the stale
null dereference and waiting for a view that never opens. Both failed in the
old code and pass after the fix. Empty playlists now stop/reset video without
waiting, and initialized refreshes recheck the current model after the await.
Video stop completion is awaited and video flags are published on the caller's
context. All 157 tests and the application Release build pass. The full native
baseline resumes with this correctness fix, before playlist preparation changes.

Three fresh native launches at 7928506b all reached the 100k playlist and timed
out while publishing its collection. Completed subphases are kept separately
from the small startup playlist. The baseline and compact comparison are in
music-playlist-ui-baseline.json and music-playlist-ui-results.md. The bound
total-duration getter sums every item on each collection notification; bulk
insertion therefore performs quadratic work. Duration notification batching
is the next optimization, after this immutable baseline is committed.

Bulk playlist replacement and append now defer only TotalPlaylistDuration
notifications until the outermost operation completes. Collection events,
occurrence identities and order are retained; single edits notify immediately.
The final notification also runs after a partial insertion failure. Two 10k-row
regressions failed in the old behavior (10,001/10,000 notifications instead of
one), then passed with batching. A third verifies exception recovery and later
single edits. All 160 tests and the Release application build pass. Native WPF
results are measured separately against the frozen timeout baseline.

The first duration-batched native pilot verified and rendered all 100k ordered
occurrences, then timed out during the final-track scrolling check. Its raw
record is iterations/music-playlist-duration-scroll-failure-f6ecdd66.json.
This is partial evidence, not a completed UI comparison. Failure-only viewport
observations and a screenshot now retain the exact scrolling state without
changing successful timing boundaries.

The scrolling diagnostic captured the correct final occurrence at the viewport
edge. The visibility check now allows 0.001 device-independent pixels of layout
rounding, far below a physical pixel. The diagnostic run and its failure stack
are retained under iterations/music-playlist-scroll-diagnostics-dc5b48b0.*.
It also exposed an uncancelled metadata worker writing to a disposed track;
that separate lifecycle defect must be fixed before accepting a full UI run.

Metadata refresh now registers its cancellation source before media analysis,
cancels the previous generation and cancels before playlist replacement,
clearing or player disposal. The worker checks cancellation before metadata
publication, observes cancelled/disposed completion and logs active failures.
Sources are removed and disposed when their work finishes. Four controlled
async regressions failed before the fix and now pass; all 164 tests and the
Release application build pass. This fixes the disposed-track task exposed by
the native pilot; the next native series will retain full rendering checks.

The repeated 95283cc8 layout-only series is retained as
iterations/music-playlist-layout-series-95283cc8.json. Its failed runs exposed
stored-song replacement racing live playlist enumeration. Stored metadata is
now queried on the worker and applied after returning to the UI context; stale
results are discarded and notification hooks are restored on failure. Debounced
search and virtual-list reload callbacks use the UI dispatcher. Both dispatcher
regressions failed before the fix and now pass; all 166 tests and the application
Release build pass.

The native music runner now waits for a composition frame and rechecks its row
predicate before each endpoint, then records painted-view proof. Query text is
chosen from the immutable incoming snapshot rather than the live collection.
The compact report requires all painted endpoints, ordered playlist/viewport
checks and stable search fingerprints; it rejects layout-only or traced data.
The older activation scope measured layout only, so painted activation has no
comparable baseline. Completed conversion/read phase boundaries stay unchanged.

With dispatcher ownership restored, the painted a69e0a61 pilot timed out after
collection publication completed. It is retained as
iterations/music-playlist-painted-timeout-a69e0a61.json. New scopes split stored
song reads/publication and collection replacement/active-item dispatch so the
remaining bottleneck is measured before changing its repeated index scans.

The 0fcece16 native control timed out inside stored-song enrichment publication;
collection replacement had completed in 10.9k ms and active-item dispatch in
4.1k ms. The new enrichment baseline is retained as
iterations/music-enrichment-baseline-0fcece16.json before replacing its index
lookup. The upcoming lookup keeps the original first-occurrence selection.

Stored-song enrichment now builds one first-occurrence index for its captured
playlist instead of scanning the live playlist for every stored song. The
original duplicate-ID selection and occurrence order are retained. Regression
checks cover duplicate/non-positive IDs, empty input and 100k rows with 50k
repeated IDs, including one ID read per row. All 168 tests and the application
Release build pass; native gains are measured against the frozen control.

The aa75995d native lookup pilot also timed out inside stored-song enrichment.
Its full record is retained as
iterations/music-enrichment-lookup-timeout-aa75995d.json. After the large stored
song read, completed artist lookups consumed 11.1k ms and album lookups 3k ms;
relationship lookups are the next measured target. The occurrence index has no
validated end-to-end timing gain yet. The results table keeps optimized music
endpoints pending and uses the frozen enrichment control for the new subphases.

Stored-song enrichment now prepares missing artist/album views in batches of
up to 256 distinct positive IDs before UI publication. It retains configured
nested relationships, existing cache identity, change subscriptions and lazy
full-library loading. A generation change discards stale batch results. Tests
cover 100k database rows/repeated playlist occurrences, bounded query reads,
concurrent preparation, filtered/missing IDs, retry, cache reset, later full
load identity and nested relationship updates. All 170 tests and the Release build pass. The native
runner also waits for stored metadata readiness before recording the painted
100k-playlist endpoint; timings remain pending until a complete native series.

The first two 17e568b1 native attempts did not reach the 100k playlist load.
They are retained in iterations/music-batch-preload-timeouts-17e568b1.json.
The first hit the process limit during startup (VLC construction took 44.1k ms).
The retry painted the initial music view, then timed out before the large read
scope opened; the clear operation currently has no scope. Neither attempt
provides a comparable enrichment or end-to-end gain. Optimized playlist values
remain pending. The next diagnostic target is clearing before the large load.

Clear/save diagnostics now distinguish the native clear endpoint, saved
snapshot creation, save queue wait, database update, player reset and final
collection clearing. The Release build passes. These opt-in scopes retain the
existing operation order and are disabled during ordinary application use;
the next native control will identify the incomplete clear stage before fixes.

The cc2bf8e2 clear control timed out while clearing waited on the playlist save
queue. An earlier save remained inside UpdatePlaylist for the original 28-row
playlist; snapshot creation took 18.5 ms initially and 4 ms for clearing. The
record is frozen in iterations/music-clear-save-wait-baseline-cc2bf8e2.json.
Further scopes will distinguish stored-data reads, reconciliation, database
writes and change publication within that save before selecting a fix.

Playlist storage diagnostics now split UpdatePlaylist into stored read,
reconciliation, playlist write, actual-item write, change notification and
logging. The Release build passes. These scopes preserve the previous query,
reconciliation and write order. They apply only with benchmark diagnostics
on; ordinary application behavior is unchanged.

The eee4b35c control narrows the save wait to its stored-playlist read: the
read scope remains active at the process limit, before reconciliation or
writes. The frozen record is
iterations/music-storage-read-timeout-eee4b35c.json. The original playlist has
28 rows. No optimization percentage is attributed to this incomplete read;
the next investigation is the SQL generated by its multiple include paths.

The read-only profile-save-read command captures the exact SingleOrDefault
SQL and its EXPLAIN plan for the original and 100k stress playlists, without
executing the materializing read. The current joined query materializes an
unfiltered PlaylistSongs/SoundItems/FileInfos subquery before filtering the
selected playlist. SQL compilation/EXPLAIN timings are diagnostic preparation
times, not playlist load measurements.

The music-playlist save read now uses EF split queries to avoid materializing
unrelated playlist entries. The legacy SQL plans are retained in
iterations/playlist-save-joined-query-plans-3c5e36d6.json. SQLite regressions use
28-row and full 100k-row queues alongside each other, checking ordered duplicate
occurrences, file metadata, private flags, actual item and indexed collection
access without a full PlaylistSongs scan. All 172 tests and the Release build pass. Native timing
gains remain unproven until a complete painted benchmark series finishes.

The complete 5be3a2c4 native series is retained in
music-playlist-ui-optimized.json: all three runs passed ordered 100k occurrence
checks, stored-metadata readiness, composition-frame checks, final-row viewport
checks and stable search IDs/counts. Their load/render times range from 32.3k
to 32.5k ms. Screenshots were inspected for populated initial/final rows, empty
no-match results and five near-match rows. The initial attempt, which timed
out in small-playlist publication before the measured large load, is retained
separately in iterations/music-save-split-initial-publication-timeout-5be3a2c4.json.

The median table now records 32.4k ms load/render, 2.3k ms stored enrichment,
222.3 ms final-row scroll and about 0.5k ms rendered searches. Prior unfinished
endpoints have no invented percentage. Startup/disposal save and clear tasks
remain visible in raw snapshots; report validity requires every measured
music/publication endpoint to finish, while excluding those separate shutdown
tasks from its active-endpoint gate. Active enrichment remains rejected.
Collection publication at 15.2k ms, including 11.3k ms replacement, is the next
largest measured loading bottleneck; it has no consistent measured gain yet.

The ff22209c collection task defers WPF Count/Item[] and collection notifications
while retaining every Rx item event, order and duplicate occurrence. Ordinary
edits retain Rx tracking after the batch, and Clear also empties the secondary
view. Four new collection regressions cover 100k entries (200k for self-append),
WPF ListCollectionView, tracking, empty append, throwing observers and enumerators.
Player duration regressions now use 100k entries. All 176 tests passed; after
adding the secondary-view clear fix, all seven affected tests passed again.
The Release build has 152 warnings and no errors.

The prior complete 5be3a2c4 baseline is frozen in
iterations/music-collection-publication-baseline-5be3a2c4.json. The ff22209c
completed native series is now music-playlist-ui-optimized.json: load/render
27k ms (-16.7%), collection publication 8.4k ms (-44.7%), replacement 4.4k ms
(-61.2%). All three completed runs passed ordered 100k occurrence, stored metadata,
composition, final-row and unchanged search fingerprint checks. Loaded/final rows
and empty/five-row searches were visually inspected. One additional launch timed
out in incoming data/view creation; it remains in
iterations/music-collection-read-timeout-ff22209c.json and is disclosed in the
compact reports. Completed-endpoint medians do not establish improved reliability.

Clear-before-load is slower (2.2k vs 662.7 ms); its collection stage is only
5-9 ms and most measured time is awaiting saves. Scroll and search medians also
increased and remain visible. No improvement is claimed for these regressions.
Remaining collection publication is 8.4k ms; saved/incoming view creation is
5.3k/5.1k ms, with duplicate conversion and dispatch still to investigate.

Regenerate the native comparison with music-playlist-ui-report.ps1 using
-Baseline performance/iterations/music-collection-publication-baseline-5be3a2c4.json
-Optimized performance/music-playlist-ui-optimized.json
-IncompleteRuns performance/iterations/music-collection-read-timeout-ff22209c.json.

The 39e8df24 task removes the incoming song-view conversion for saved-playlist
PlayFromPlaylist, PlayFromPlaylistLast and InitSetPlaylist events. PlayPlaylist
still constructs displayed views from authoritative rows ordered by position and
row ID. The incoming rows remain available for IDs/source in enrichment; they
previously produced a second set of song views which was immediately discarded.
Ordinary Play/Add retain construction and copying of position, favorite, playing,
selection and duration state. Five new regressions use 100k-entry payloads; the
three saved-playlist cases failed before the fast path. All 181 tests passed and
the Release build completed with 152 warnings and no errors.

The immediate ff22209c baseline is frozen in
iterations/music-incoming-views-baseline-ff22209c.json. Its task comparison is
iterations/music-incoming-views-results-39e8df24.md: conversion 5.1k to 3.81 ms
(-99.9%), load/render 27k to 23.1k ms (-14.2%). All three native runs completed,
including ordered 100k occurrences, stored metadata readiness, composition frames,
final-row viewport checks and unchanged search inputs/result fingerprints.
Loaded/final rows were inspected for each run, alongside empty and five-row
search results. Full load/render samples range from 21.7k to 27.1k ms; the slowest
is retained in the series. Native startup/disposal save scopes remain in raw JSON.
No new startup reliability claim is made.

The main music table keeps the first complete painted 5be3a2c4 baseline, so it
shows cumulative load/render 32.4k to 23.1k ms (-28.5%). Its original earlier
unfinished baselines and the ff22209c read timeout remain in the iterations/raw
evidence. Clear-before-load is now 3k ms (+36.1% against ff22209c), collection
publication 9.1k ms (+7.8%) and saved view construction 5.9k ms (+10.7%); these
slower timings stay visible. Collection publication remains the largest measured
loading stage; its replacement and dispatch stages are 5.2k and 4.2k ms.

Regenerate the main native table with music-playlist-ui-report.ps1 using
-Baseline performance/iterations/music-collection-publication-baseline-5be3a2c4.json
-Optimized performance/music-playlist-ui-optimized.json. Regenerate this task's
comparison with -Baseline performance/iterations/music-incoming-views-baseline-ff22209c.json
-Optimized performance/music-playlist-ui-optimized.json
-Output performance/iterations/music-incoming-views-results-39e8df24.md.

The d467b7ad storage task replaces nested per-row reconciliation with row-ID
lookups, reads the parent and ordered rows separately, and writes at most 512
tracked rows per batch. Parent metadata, removals, updates, inserts and playback
reference writes share one transaction; the context is disposed and its change
tracking setting restored. Four new 100k-row regressions cover complete deletion,
reversal with duplicate tracks, half replacement with generated playback identity,
and rollback after successful batches. Empty input previously retained every row.
All 185 tests passed; Release x64 built with 152 warnings and no errors.

The native save endpoint moves the first item to the end and checks the planned
row IDs, track IDs and positions in SQLite. Clear saves the queue before clearing
the displayed list; its persisted queue deliberately remains restorable. Both
endpoints wait for composition and verify all 100k persisted occurrences.
The extended 180-second baseline is music-playlist-write-baseline.json at 3b70d7dc:
save reached reconciliation and timed out; clear completed in 66.9k ms. Earlier
60-second attempts stalled before the write workload. Their untouched snapshots
remain in iterations/music-write-initial-*.json and music-write-retry-*.json.
The initial clear runner also failed reading a just-terminated process's file;
its raw early Rendered status does not establish a completed workload. 88725f0a
waits for termination before reading; 3b70d7dc extends the write process budget.
Different budgets and unfinished endpoints are never compared as timing gains.

music-playlist-write-optimized.json retains the d467b7ad native series.
music-playlist-write-report.ps1 verifies operation flags, fixture/runtime/machine,
180-second protocol, painted workload/result and persisted occurrence checks.
Completed save/render is 27.6k ms without a percentage; clear is 15.3k ms (-77.1%).
All slower samples remain. Shutdown save scopes after the verified save endpoint
remain in raw JSON and do not define the reported endpoint. Queue waits and
ordinary media/metadata work remain part of native operation; background CPU and
paging activity were observed during baseline collection. These are measurements
on this machine, rather than an isolated estimate of one code change's effect.

The plain music series was also rerun. Its immediate comparison is
iterations/music-playlist-write-load-results-d467b7ad.md, with the prior 39e8df24
series frozen alongside it. Load/render is slower: 23.1k to 26.4k ms (+13.9%);
scroll and searches also slowed. The main table retains its first complete
5be3a2c4 baseline, so cumulative load/render is 32.4k to 26.4k ms (-18.6%).
All plain runs retain ordered occurrence, metadata, composition, final-row and
search fingerprint checks. Loaded and final viewports were inspected for every
plain run, alongside empty/five-row searches and the saved/cleared write results.
One loaded viewport showed the media Buffering overlay while playlist rows were
ready; the playlist endpoint does not assert completed audio playback.

Regenerate write results with music-playlist-write-report.ps1. The main report.ps1
imports those compact rows. Regenerate the current music table with
music-playlist-ui-report.ps1 -Baseline
performance/iterations/music-collection-publication-baseline-5be3a2c4.json
-Optimized performance/music-playlist-ui-optimized.json.
A follow-up regression supplies 100k unchanged rows without referenced metadata.
It failed on d467b7ad because the returned row list used the bare input instead of
the authoritative stored rows. Unchanged saves now retain stored rows, and a bare
playback reference is repaired before notification. Every returned row and the
playback reference are checked, along with unchanged persisted IDs/order and
context cleanup. All 186 tests passed; Release built with 152 warnings, no errors.
The 074136ae native attempts remain in
iterations/music-playlist-metadata-native-attempts-074136ae.json. One clear launch
missed the unchanged 20-second initial-render deadline; another reported an
unobserved NullReferenceException from UpdateVlcTime's delayed playback save.
Completed samples in that attempted series are retained without concealing failures.
The prior d467b7ad write and plain-load series are frozen in the adjacent files.

The playback callback now captures its track before the delay, skips a stale
track/disposed player, persists the captured model across the playlist-save await,
and rechecks track identity before a queued UI notification. Background write
exceptions are observed and logged. Nine tests use 100k-row queues and controlled
clear/switch, queued notification, disposal and write failure boundaries; the six
original race/failure cases failed before correction. The normal current-track
notification remains covered. All 195 tests passed; Release built with 152 warnings
and no errors. No render deadline or workload was relaxed.
The verified 09ea4d79 rerun is now the current optimized JSON series. Save/render
is 28.6k ms (the original save baseline timed out); save/clear/render is 14.3k ms
versus 66.9k ms (-78.6%). All final write launches completed their painted endpoints
and exact persisted occurrence checks. The plain music series also completed
ordered/metadata/composition/final-row and unchanged search fingerprint checks.
Loaded and final rows were inspected for every plain launch, plus empty/five-row
search results and saved/cleared write results. Prior 074136ae failures remain
explicitly linked from the write report; completed medians do not prove reliability.

The follow-up comparisons against the last complete d467b7ad series are
iterations/music-playlist-playback-save-write-results-09ea4d79.md and
iterations/music-playlist-playback-save-load-results-09ea4d79.md. Save is slower
(+3.7%), clear faster (-6.7%), and load/render 26.4k to 24.8k ms (-5.8%). Incoming
read/conversion, active-item dispatch and near-match search are slower in that
series and remain visible. The main music table keeps the first complete 5be3a2c4
baseline, with cumulative load/render 32.4k to 24.8k ms. The largest measured music
operation remains reorder/save/render; collection publication remains the largest
loading stage. No other UI category is claimed complete by these checks.
The current optimization scope is listed in [focus.md](focus.md). Local test release
7.6.9771.25313 was deployed with the existing publish profile at b37ccf86, with
application backups and protected-data hashes retained under artifacts/local-deployment.

At 2abd4a10, playlist loading/track selection uses detached copies of its known
stored row, track and file-info graph instead of BinaryFormatter serialization.
The frozen 100k-entry snapshot baseline and optimized samples are in
playlist-snapshot-{baseline,optimized}.json; [component results](playlist-snapshot-results.md)
separate the first snapshot, repeats and allocation. The native
[snapshot task comparison](iterations/music-playlist-snapshot-load-results-2abd4a10.md)
compares all complete painted runs with 09ea4d79 and retains slower stages.
The main music table continues using its first complete 5be3a2c4 baseline.

All 201 tests passed, including six snapshot checks for huge duplicate queues,
occurrence identity, scalar metadata, detached mutation, missing data and unknown
derived graphs. Release app compilation passed. Native row order, stored metadata,
final-row visibility and search fingerprints passed; first/final viewport images
were reviewed. These are fresh-process measurements, not a post-restart cold-disk
baseline, and this task does not establish rapid-skip/media-playback correctness.
The remaining largest completed load phases are saved row view creation (5k ms)
and collection replacement (4.5k ms); rapid-skip persistence and album detail
baselines remain required under the current focus.

Rapid playlist selection now rejects superseded preparation and initialization,
keeps native media calls on workers, publishes current state through the UI
context, and wraps previous from the first occurrence to the last valid index.
Twelve controlled checks use 100k-entry playlists; the original four failures
and compact comparison are retained in [selection results](playback-selection-results.md).
All 213 tests passed, and the Release app built with no errors (374 warnings).
The media device is mocked in these checks: native skip timing, background
playlist saves and remote-media cancellation still need their own baselines.

Native rendering checks at ebff741c retained a 60-second timeout during saved
playlist view creation. Two launches completed all ordered rows, stored metadata,
painted first/final viewport and unchanged fuzzy-search fingerprints; the four
playlist viewport screenshots were reviewed. Completed load/render endpoints
were 16.6k ms versus 19.8k ms at 2abd4a10 (-16%); scrolling (+2.7%) and near-match
search (+22.6%) were slower. These observations do not prove a consistent speed
improvement or native rapid-skip correctness. [Task comparison](iterations/music-playlist-selection-load-results-ebff741c.md)
retains the timeout and all slower stages; the main table still uses the first
complete 5be3a2c4 baseline. Report validation now recognizes an unfinished saved
view-creation stage and checks search fingerprints even when the first run fails.
Malformed timeout evidence and changed fingerprints were rejected in guard checks.
The largest unfinished load stage is saved playlist view creation; native skip
persistence, albums, cold startup, lyrics and explorer work remain within focus.

Saved playlist construction at 171b2af1 resolves shared dependencies once per batch,
while retaining a distinct song wrapper, view model, nested factory and transient
window manager for each occurrence. Custom bindings and activation hooks retain
container resolution and its original parent context. The controlled 100k-entry
comparison uses real Ninject, eight shared stub services and a real WindowManager
per entry; database reads, rendering, verification and disposal are excluded.
[Component results](saved-playlist-views-results.md) retain first/repeated timings
and allocations. The earlier all-shared-service control is frozen in iterations/;
it did not represent the application's transient window binding.

The final native series confirmed batch construction and per-item window creation
in every run. Saved-view creation fell from 4.3k to 130.6 ms (-97%); total load/render
fell from 16.6k to 15.9k ms (-4.7%). The preceding baseline timeout and every slower
stage remain in the [task comparison](iterations/music-playlist-saved-views-load-results-171b2af1.md).
All three final runs completed ordered 100k occurrences, stored metadata, painted
first/final rows and unchanged search fingerprints. The six viewport screenshots
were visually reviewed. Fresh profiles do not establish cold-disk performance.

All 218 tests passed; Release built with 152 warnings and no errors. Tests preserve
occurrence identity, independent state, per-item service lifetimes and custom
activation parent contexts. The failed intermediate parent-context regression is
explicitly labeled as an uncommitted draft in iterations/. Test/build evidence is
in iterations/saved-views-tests-171b2af1.txt. The main music report keeps its first
complete 5be3a2c4 baseline. Enrichment and collection publication, each about 4.7k ms,
are now the largest completed load stages. Other areas in focus.md remain pending;
the locally installed test version remains 7.6.9771.25313.

[Startup window ordering](window-startup-results.md) covers the startup focus bug
at 9cfd8889. The application creates the same shell/regions/view model and optional
saved position locally, avoiding VCore's private loaded handler that toggles Topmost
and replaces its binding. Splash closure runs once for the shell's own Loaded event.
Video and control surfaces show without activation, establish ownership before showing,
and keep an inactive owner group below the foreground application. Explicit topmost
and activation remain covered. This follows the native ordering/activation flags in
[SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos).

All 223 tests passed; Release app build: 152 warnings, zero errors. The original
overlay failed the automatic-activation test. Final native checks use separate normal
and topmost foreground processes, verify native stacking and topmost flags, and run
100 layout updates and 10 visibility cycles per case. They exclude media playback
and library loading. The initial probe-file sharing failure is retained as a runner
draft failure; marker publication is atomic and temporary markers are removed.

The complete application then rendered the copied 207k library and 100k playlist;
ordered occurrences, stored metadata, first/final rows and search endpoints passed.
Home and first/final playlist images were reviewed. This single smoke launch has no
performance percentage or cold-disk claim. Raw launch/screenshot data remains local
under artifacts/performance/runs/window-startup-smoke-20261002-172018. The deployed
local test release is still 7.6.9771.25313; this fix is currently on the branch.
