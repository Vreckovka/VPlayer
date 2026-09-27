using System.Text.Json;
using VPlayer.AudioStorage.DomainClasses;
static Row R(int row, int track, int order, bool available = true) => new() { Id = row, Track = track, Order = order, Available = available };
static void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS: " + name); }
static List<Row> Reorder(Row[] rows, params int[] ids) => PlaylistOrder.ReorderAvailable(rows.OrderBy(x => x.Order).ThenBy(x => x.Id), ids, x => x.Track, x => x.Available);
static bool Changed(IEnumerable<Row> old, IEnumerable<Row> next) => PlaylistOrder.HasChanges(old, next, x => x.Id, x => x.Track, x => x.Order);
var original = new[] { R(1,10,1), R(2,20,2), R(3,30,3) };
var moved = Reorder(original,30,10,20);
Check(moved.Select(x=>x.Id).SequenceEqual(new[]{3,1,2}), "move last to first");
Check(Reorder(original,20,30,10).Select(x=>x.Id).SequenceEqual(new[]{2,3,1}), "move first to last");
var missing = new[] { R(1,10,1), R(2,20,2,false), R(3,30,3), R(4,40,4,false) };
var result = Reorder(missing,30,10);
Check(result.Select(x=>x.Id).SequenceEqual(new[]{3,2,1,4}), "unavailable tracks retain saved slots");
Check(Reorder(new[]{R(1,10,1),R(2,10,2),R(3,30,3)},10,30,10).Select(x=>x.Id).SequenceEqual(new[]{1,3,2}), "duplicate occurrences retain separate row identities");
foreach(var bad in new[]{new[]{10},new[]{10,20,99},new[]{10,10,20,30}})
{
 bool rejected=false; try { Reorder(original,bad); } catch(InvalidOperationException){rejected=true;}
 Check(rejected,"partial/stale queue rejected: " + string.Join(",",bad));
}
Check(!Changed(original,original.Reverse()),"database enumeration order ignored");
Check(Changed(original,new[]{R(3,30,1),R(1,10,2),R(2,20,3)}),"same-count reorder detected independently of hash");
Check(Changed(original,new[]{R(1,10,1),R(2,99,2),R(3,30,3)}),"same-count replacement detected");
Check(Changed(original,original.Take(2)),"removal detected");
for(int i=0;i<result.Count;i++) result[i].Order=i+1;
var loaded=JsonSerializer.Deserialize<List<Row>>(JsonSerializer.Serialize(result))!.OrderBy(x=>x.Order).ToList();
Check(loaded.Select(x=>x.Id).SequenceEqual(new[]{3,2,1,4}),"saved order survives serialization and reload");
loaded.ForEach(x=>x.Available=true);
Check(loaded.Select(x=>x.Track).SequenceEqual(new[]{30,20,10,40}),"later-resolved tracks return at preserved positions");
Console.WriteLine("All 13 playlist ordering checks passed.");
class Row { public int Id {get;set;} public int Track {get;set;} public int Order {get;set;} public bool Available {get;set;} }
