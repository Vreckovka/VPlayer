using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VPlayer.AudioStorage.DomainClasses;
using VPLayer.Domain.Diagnostics;

namespace VPlayer.AudioStorage.AudioDatabase
{
  public static class PlaylistQueries
  {
    public static IQueryable<T> Public<T>(IQueryable<T> query) where T:class,IPlaylist =>
      query.OrderByDescending(x=>x.LastPlayed).Where(x=>!x.IsPrivate);

    // Compile the same async query shape as the library loader without advancing
    // its enumerator. This prepares metadata/translation without reading rows.
    public static Task<bool> PrepareAsync(Func<AudioDatabaseContext> createContext=null) => Task.Run(async () =>
    {
      try
      {
        using var measurement=StartupMeasurements.Measure("Application / playlist query preparation");
        using var context=(createContext ?? (()=>new AudioDatabaseContext()))();
        await using var enumerator=Public(context.Set<SoundItemFilePlaylist>().AsNoTracking())
          .AsAsyncEnumerable().GetAsyncEnumerator();
        return true;
      }
      catch
      {
        // Preparation is optional; the normal load retains its error handling.
        return false;
      }
    });
  }
}
