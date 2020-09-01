using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.AudioStorage.Repositories
{
  public class AlbumsRepository : GenericRepository<AudioDatabaseContext, Album>
  {
    public AlbumsRepository(AudioDatabaseContext context) : base(context)
    {
    }
  }
}