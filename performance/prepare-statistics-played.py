"""Create an immutable all-played Statistics stress fixture from the library copy."""
import hashlib
import json
import sqlite3
import sys
from datetime import datetime, timezone
from pathlib import Path

parent, target = (Path(value).resolve() for value in sys.argv[1:3])
source = parent / "VPlayerDatabase.db"
metadata = json.loads((parent / "fixture.json").read_text(encoding="utf-8-sig"))
def digest(path):
    with path.open("rb") as data:
        return hashlib.file_digest(data, "sha256").hexdigest().upper()
if digest(source) != metadata["DatabaseSha256"]:
    raise RuntimeError("Parent fixture changed.")
if target.exists():
    raise RuntimeError("Target already exists; fixtures are immutable.")
target.mkdir(parents=True)
database = target / "VPlayerDatabase.db"
with sqlite3.connect(source.as_uri() + "?mode=ro", uri=True) as original:
    with sqlite3.connect(database) as copy:
        original.backup(copy)
        # One day plus unique subsecond ticks; increasing IDs exercise worst-case
        # leaderboard insertions. All public sounds are played and need totaling.
        if copy.execute("SELECT max(Id) FROM SoundItems").fetchone()[0] >= 10_000_000:
            raise RuntimeError("Sound IDs exceed the unique fractional-tick range.")
        copy.execute("UPDATE SoundItems SET TimePlayed = '1.00:00:00.' || printf('%07d', Id)")
        copy.commit()
        count, zero = copy.execute("SELECT count(*), sum(TimePlayed='00:00:00') FROM SoundItems").fetchone()
        if count != metadata["SoundItems"] or zero:
            raise RuntimeError("All-played fixture verification failed.")
metadata.update(
    CreatedUtc=datetime.now(timezone.utc).isoformat(),
    ParentSha256=metadata["DatabaseSha256"],
    DatabaseSha256=digest(database),
    UniqueSoundTimePlayed=True,
    AllSoundsPlayed=True,
)
(target / "fixture.json").write_text(json.dumps(metadata, indent=2), encoding="utf-8")
if digest(source) != metadata["ParentSha256"]:
    raise RuntimeError("Parent changed during preparation.")
print(f"Prepared {count} played sounds with unique times and file metadata.")
