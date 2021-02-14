using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.AudioStorage.Repositories
{
  public class PlaylistsRepository : GenericRepository<AudioDatabaseContext, SongsPlaylist>
  {
    public PlaylistsRepository(AudioDatabaseContext context) : base(context)
    {
    }
  }
}