using LibVLCSharp.Shared.Structures;

namespace VPlayer.WindowsPlayer.ViewModels.VideoProperties
{
  public abstract class LanguageVideoProperty : VideoProperty
  {
    protected LanguageVideoProperty(TrackDescription model)
    {
      Model = model;
    }

    #region Model

    public TrackDescription Model{ get; }

    #endregion

    #region Description

    public override string Description => Model.Name;

    #endregion

    #region Language

    private string myVar;

    public string Language
    {
      get { return myVar; }
      set
      {
        if (value != myVar)
        {
          myVar = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }
}