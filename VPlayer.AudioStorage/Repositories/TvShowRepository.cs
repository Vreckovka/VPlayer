using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;

namespace VPlayer.AudioStorage.Repositories
{
  public class TvShowRepository : GenericRepository<AudioDatabaseContext, TvShow>
  {
    public TvShowRepository(AudioDatabaseContext context) : base(context)
    {
    }
  }

  public class TvShowPlaylistRepository : GenericRepository<AudioDatabaseContext, VideoFilePlaylist>
  {
    public TvShowPlaylistRepository(AudioDatabaseContext context) : base(context)
    {
    }
  }
}