using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.AudioStorage.Repositories
{
  public class PlaylistsRepository : GenericRepository<AudioDatabaseContext, Playlist>
  {
    public PlaylistsRepository(AudioDatabaseContext context) : base(context)
    {
    }
  }
}