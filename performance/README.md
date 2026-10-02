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
