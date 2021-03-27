namespace VPlayer.AudioStorage.DomainClasses
{
  public interface IEntity
  {
    #region Properties

    int Id { get; set; }

    #endregion Properties
  }

  public interface INamedEntity : IEntity
  {
    #region Properties

    string Name { get; set; }

    #endregion Properties
  }

  public interface DownloadableEntity : IEntity
  {
    InfoDownloadStatus InfoDownloadStatus { get; set; }
  }
}