using VPlayer.AudioStorage.AudioDatabase;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.AudioStorage.Repositories
{
  public class ArtistRepository : GenericRepository<AudioDatabaseContext, Artist>
  {
    public ArtistRepository(AudioDatabaseContext context) : base(context)
    {
    }
  }
}