using LibVLCSharp.Shared.Structures;

namespace VPlayer.WindowsPlayer.ViewModels.VideoProperties
{
  public abstract class LanguageVideoProperty : VideoProperty
  {
    protected LanguageVideoProperty(TrackDescription model) : base(model)
    {
      Description = model.Name;
    }

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