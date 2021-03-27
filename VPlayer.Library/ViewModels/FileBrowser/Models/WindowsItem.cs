using System;
using VCore.Annotations;
using VCore.Standard.ViewModels.TreeView;

namespace VPlayer.Library.ViewModels.FileBrowser
{

  public class WindowsItem<TModel> : TreeViewItemViewModel where TModel : class
  {
   public WindowsItem([NotNull] TModel model)
    {
      Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    #region Model

    private TModel model;

    public TModel Model
    {
      get { return model; }
      set
      {
        if (value != model)
        {
          model = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }

}