# Statistics index experiment

Experimental copies of the fully played 207k-item fixture; application code is unchanged.

| Baseline now | Experimental indexes |
| --- | --- |
| **first Statistics data load** — 1.7k ms | 1.7k ms (+1.1%) |
| **warm Statistics data load** — 513.1 ms | 496.9 ms (-3.2%) |

The warm-load gain is modest; these data-only measurements do not establish
a fix for the longer UI waits. These indexes are
**not included in the application**. They added about 7.4 MB and took 791 ms
to create on the disposable copy.

The candidate indexes cover Id, IsPrivate and TimePlayed on sounds, videos and
episodes. Raw query checks verified identical rows and order. Independent
production Statistics processes alternated baseline/indexed order; all valid paired samples
are retained. First-load medians are separate from pooled warm-load medians.
Preparation closes SQLite connections before checksumming.

[Raw results](statistics-index-probe-6a2089b5.json).
Reproduce the query experiment with
[probe-statistics-indexes.py](../probe-statistics-indexes.py), passing a source
fixture directory and a new output directory.