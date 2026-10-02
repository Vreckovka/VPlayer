# File browser search

| Baseline | Optimized |
| --- | --- |
| First recursive search: 23.3k ms | Pending |
| Search in loaded folders: 3.6k ms | Pending |
| Deep folder search: 2.5k ms | Pending |
| Rapid query changes: 11.8k ms; wrong results | Pending |

Preliminary measurements cover 101k files named from the copied library and 133 folders, including 33 levels. All three browser regressions reproduced: null/empty search before opening a folder and an unchanged top-level tree after filtering. Rapid queries and clearing an in-flight query also left incorrect results.

The final comparison adds a query matching every generated track and separately measures recursive directory loading. It uses the app's default factory metadata and window-manager lifetime. This component benchmark exercises the actual browser/folder/file view models; it does not measure rendered full-player UI or audio playback.
