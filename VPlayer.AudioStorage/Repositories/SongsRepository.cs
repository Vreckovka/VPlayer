using VCore.Annotations;
using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.AudioStorage.Repositories
{
  public class SongsRepository : GenericRepository<AudioDatabaseContext, Song>
  {
    public SongsRepository(AudioDatabaseContext context) : base(context)
    {
    }
  }
}