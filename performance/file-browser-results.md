# File browser search

| Baseline | Optimized |
| --- | --- |
| Recursive directory load: 44.6k ms | Pending |
| First recursive search: 34.0k ms | Pending |
| Search matching all tracks: 19.5k ms | Pending |
| Rapid query changes: 14.9k ms; wrong results | Pending |
| Search in loaded folders: 5.2k ms | Pending |
| Deep folder search: 3.2k ms | Pending |
| Recursive load UI allocation: 1.5 GiB | Pending |

101k files named from the copied library, 133 folders and 33 levels. The baseline uses the app's default factory metadata and transient window-manager lifetime. All three browser regressions reproduced; rapid queries and clearing an in-flight query also left incorrect results. The top-level tree retained unfiltered items.

This component benchmark exercises the actual browser/folder/file view models. It does not measure rendered full-player UI or audio playback. The separate load measurement retains the first browser and creates a second one, exposing memory pressure. The earlier protocol lacked an all-track query and used a shared mock window manager; its immutable preliminary results remain in `iterations/file-browser-preliminary-be60ac1a.json`.
