using LibVLCSharp.Shared.Structures;

namespace VPlayer.WindowsPlayer.ViewModels.VideoProperties
{
  public enum Language
  {
    Other,
    Czech,
    Enghlish,
    Japanese,
    
  }

  public abstract class LanguageVideoProperty : VideoProperty
  {
    protected LanguageVideoProperty(TrackDescription model)
    {
      Model = model;
    }

    #region Model

    public TrackDescription Model{ get; }

    #endregion

    public override int OrderNumber => Model.Id;

    #region Description

    public override string Description => Model.Name;

    #endregion

    #region Language

    private Language language;

    public Language Language
    {
      get { return language; }
      set
      {
        if (value != language)
        {
          language = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }
}