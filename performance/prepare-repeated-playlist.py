import sys,sqlite3,json,hashlib,pathlib,datetime
source=pathlib.Path(sys.argv[1]).resolve()
out=pathlib.Path(sys.argv[2]).resolve()
out.mkdir()
database=out/'VPlayerDatabase.db'
metadata=json.loads(source.joinpath('fixture.json').read_text(encoding='utf-8-sig'))
assert hashlib.sha256(source.joinpath('VPlayerDatabase.db').read_bytes()).hexdigest().upper()==metadata['DatabaseSha256'], 'Source fixture changed'
with sqlite3.connect(source.joinpath('VPlayerDatabase.db').as_uri()+'?mode=ro',uri=True) as original:
    with sqlite3.connect(database) as copy:
        original.backup(copy)
        rows=copy.execute('SELECT Id,IdReferencedItem FROM PlaylistSongs WHERE SoundItemFilePlaylistId=658 ORDER BY OrderInPlaylist,Id').fetchall()
        assert len(rows)==100000
        assert len(set(row[1] for row in rows))==100000
        copy.executemany('UPDATE PlaylistSongs SET IdReferencedItem=? WHERE Id=?',((rows[i-50000][1],rows[i][0]) for i in range(50000,100000)))
        assert copy.execute('SELECT count(*),count(DISTINCT IdReferencedItem) FROM PlaylistSongs WHERE SoundItemFilePlaylistId=658').fetchone()==(100000,50000)
metadata=json.loads(source.joinpath('fixture.json').read_text(encoding='utf-8-sig'))
metadata['OriginalDatabaseSha256']=metadata['DatabaseSha256']
metadata['DatabaseSha256']=hashlib.sha256(database.read_bytes()).hexdigest().upper()
metadata['StressPlaylistId']=658
metadata['StressEntries']=100000
metadata['StressDuplicateTracks']=50000
metadata['CreatedUtc']=datetime.datetime.now(datetime.timezone.utc).isoformat()
out.joinpath('fixture.json').write_text(json.dumps(metadata,indent=2),encoding='utf-8')
print(json.dumps({'Fixture':str(out),'Entries':100000,'DuplicateTracks':50000,'Sha256':metadata['DatabaseSha256']}))