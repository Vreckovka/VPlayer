using VCore.Standard;

namespace VPlayer.Home.ViewModels
{
  public class DetailItemViewModel<TModel> : ViewModel<TModel>
  {
    public DetailItemViewModel(TModel model) : base(model)
    {
    }

    #region Info

    private string info;

    public string Info
    {
      get { return info; }
      set
      {
        if (value != info)
        {
          info = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  }
}