# Grouped playlist UI — worst-case comparison

Same disposable expanded library: 207 110 sound items, 5 310 playlists, including 5,000 additional favorites with long titles. Release x64, visible WPF windows, fresh profile per launch.

| Baseline now | New optimized version |
| --- | --- |
| Complete render and last-favorite scroll: 0/7 completed; 7 timed out at 60 s; 0 failed | Pending |
| Populated playlist view: Not reached (0/7) | Pending |
| First window render: 3 631,23 ms median; 4 016,86 ms max; 7/7 reached | Pending |
| Scroll to last favorite: Not reached (0/7) | Pending |
| Realized playlist rows at ready: Not reached | Pending |
| Realized playlist rows after scroll: Not reached | Pending |
| Commit: 64a18e1fcecbc33ccf3254968ca5975d6de2bd3b | Pending |

Fixture SHA-256: 83DD822EAA4C7E9F8FF1BD06511949553B4562561D667BC38AE2E777A439B145.

The ready milestone requires a real nonempty rendered row. The scroll measurement requires the last favorite row to fit inside its scroll viewport. Screenshot files verify the actual application template locally. Counts include all realized row containers in the grouped view.

A timeout is a failed workload, not a timing improvement. First-frame durations from launches that later time out remain partial observations; they do not establish a successful startup. Baseline and optimized samples must match fixture, runtime, configuration, machine and window mode. Durations include process startup; the scroll scope excludes screenshot capture.

This scenario measures the grouped music-playlist view. Other app features retain their separate baselines and pending coverage in results.md.
