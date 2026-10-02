# File browser search

| Baseline | Optimized |
| --- | --- |
| Recursive directory load: 44.6k ms | 35.1k ms (-21.4%) |
| First recursive search: 34.0k ms | 28.4k ms (-16.5%) |
| Search matching all tracks: 19.5k ms | 20.4k ms (+5.1%) |
| Rapid query changes: 14.9k ms; wrong results | 16.3k ms (+9.5%); still wrong |
| Search in loaded folders: 5.2k ms | 4.4k ms (-14.3%) |
| Deep folder search: 3.2k ms | 3.2k ms (+0.7%) |
| Uppercase folder search: 2.7k ms | 2.9k ms (+6.2%) |
| Root directory load: 377 ms | 262 ms (-30.5%) |
| Recursive load UI allocation: 1.5 GiB | 0.47 GiB (-69.4%) |

101k files named from the copied library, 133 folders and 33 levels. This first optimization reduces repeated dependency resolution when creating file rows. It preserves custom bindings and default service lifetimes; 51 focused tests pass. Search correctness is the next task: rapid queries, clearing, null input and the top-level tree remain broken.

Actual browser/folder/file view models; rendered full-player UI and audio are not measured. The separate load test retains one browser and loads a second, exposing memory pressure. These are single before/after samples; timings include scheduling and cache effects. Raw results and payload hashes are retained in `iterations/file-browser-*`. The earlier preliminary protocol is excluded from this comparison.