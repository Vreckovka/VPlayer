"""Isolated index experiment; never writes to the source fixture."""
from contextlib import closing
import hashlib
import json
import sqlite3
import statistics
import time
from datetime import datetime, timezone
from pathlib import Path
import sys

parent, target = [Path(x).resolve() for x in sys.argv[1:3]]
source = parent / "VPlayerDatabase.db"
metadata = json.loads((parent / "fixture.json").read_text(encoding="utf-8-sig"))
def digest(path):
    with path.open("rb") as data:
        return hashlib.file_digest(data, "sha256").hexdigest().upper()
if digest(source) != metadata["DatabaseSha256"]:
    raise RuntimeError("Source fixture changed")
if target.exists():
    raise RuntimeError("Experiment already exists")
target.mkdir(parents=True)
indexed = target / "indexed"
indexed.mkdir()
copy_path = indexed / "VPlayerDatabase.db"
with closing(sqlite3.connect(source.as_uri() + "?mode=ro", uri=True)) as original:
    with closing(sqlite3.connect(copy_path)) as copy:
        original.backup(copy)
        begin = time.perf_counter()
        for table in ("SoundItems", "VideoItems", "TvShowEpisodes"):
            copy.execute(f'CREATE INDEX "IX_Probe_{table}_Scores" ON "{table}" ("Id", "IsPrivate", "TimePlayed")')
        copy.commit()
        index_ms = 1000 * (time.perf_counter() - begin)
child_metadata = dict(metadata, ParentSha256=metadata["DatabaseSha256"],
                      DatabaseSha256=digest(copy_path),
                      ExperimentalIndexes=True,
                      CreatedUtc=datetime.now(timezone.utc).isoformat())
(indexed / "fixture.json").write_text(json.dumps(child_metadata, indent=2), encoding="utf-8")
queries = {table: f'SELECT Id, TimePlayed FROM "{table}" WHERE NOT IsPrivate'
           for table in ("SoundItems", "VideoItems", "TvShowEpisodes")}
queries["Sound prefix"] = queries["SoundItems"] + " ORDER BY Id LIMIT 30"
result = dict(SourceSha256=metadata["DatabaseSha256"],
              IndexedSha256=child_metadata["DatabaseSha256"],
              CreateIndexesMilliseconds=index_ms,
              OriginalBytes=source.stat().st_size, IndexedBytes=copy_path.stat().st_size,
              Samples={}, Plans={}, CreatedUtc=datetime.now(timezone.utc).isoformat(),
              PythonVersion=sys.version, SqliteVersion=sqlite3.sqlite_version)
reference = {}
for label, path in (("baseline", source), ("indexed", copy_path)):
    with closing(sqlite3.connect(path.as_uri() + "?mode=ro", uri=True)) as connection:
        result["Plans"][label] = {name: connection.execute("EXPLAIN QUERY PLAN " + sql).fetchall()
                                 for name, sql in queries.items()}
        for name, sql in queries.items():
            rows = connection.execute(sql).fetchall()
            # Order of score streams must match too: tied leaderboards retain IDs.
            signature = hashlib.sha256(repr(rows).encode("utf-8")).hexdigest()
            if label == "baseline":
                reference[name] = (len(rows), signature)
            elif reference[name] != (len(rows), signature):
                raise RuntimeError("Index changes score rows or order: " + name)
result["Rows"] = {name: dict(Count=value[0], OrderedRowsSha256=value[1])
                  for name, value in reference.items()}
for iteration in range(12):
    # Alternate the first database to reduce systematic filesystem-cache bias.
    order = [("baseline", source), ("indexed", copy_path)]
    if iteration % 2:
        order.reverse()
    for label, path in order:
        with closing(sqlite3.connect(path.as_uri() + "?mode=ro", uri=True)) as connection:
            for name, sql in queries.items():
                begin, cpu_begin = time.perf_counter(), time.process_time()
                rows = connection.execute(sql).fetchall()
                cpu_ms = 1000 * (time.process_time() - cpu_begin)
                wall_ms = 1000 * (time.perf_counter() - begin)
                if len(rows) != reference[name][0]:
                    raise RuntimeError("Row count changed")
                result["Samples"].setdefault(name, {}).setdefault(label, []).append(
                    dict(WallMilliseconds=wall_ms, CpuMilliseconds=cpu_ms, Rows=len(rows)))
result["Summary"] = {name: {label: statistics.median(sample["WallMilliseconds"] for sample in samples)
                           for label, samples in modes.items()}
                     for name, modes in result["Samples"].items()}
if digest(source) != metadata["DatabaseSha256"]:
    raise RuntimeError("Source changed during experiment")
if digest(copy_path) != child_metadata["DatabaseSha256"]:
    raise RuntimeError("Indexed fixture changed during experiment")
(target / "query-probe.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
print(json.dumps({key: result[key] for key in ("CreateIndexesMilliseconds", "OriginalBytes", "IndexedBytes", "Plans", "Summary")}, indent=2))