using Windows.UI.Xaml.Documents;

namespace VPlayer.AudioStorage.DomainClasses
{
  public class AudioInfo
  {
    #region Constructors

    public AudioInfo()
    {
    }

    #endregion Constructors

    #region Properties

    public string Album { get; set; }
    public string Artist { get; set; }
    public string ArtistMbid { get; set; }
    public string DiskLocation { get; set; }

    /// <summary>
    /// Audio duration in seconds
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Audio fingerprint
    /// </summary>
    public string FingerPrint { get; set; }

    public string Title { get; set; }

    #endregion Properties

    #region Methods

    public override string ToString()
    {
      return $"{Title}|{Album}";
    }

    #endregion Methods
  }
}