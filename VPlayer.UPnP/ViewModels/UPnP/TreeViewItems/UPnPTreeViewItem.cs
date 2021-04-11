using System;
using VCore.Standard.ViewModels.TreeView;

namespace VPlayer.UPnP.ViewModels.UPnP.TreeViewItems
{
  public abstract class UPnPTreeViewItem<TModel> : UPnPTreeViewItem  where TModel : class
  { 
    public UPnPTreeViewItem(TModel model) 
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

  public abstract class UPnPTreeViewItem : TreeViewItemViewModel 
  {
   

    public UPnPTreeViewItem()
    {
     
    }

    #region IsBusy

    private bool isBusy;

    public bool IsBusy
    {
      get { return isBusy; }
      set
      {
        if (value != isBusy)
        {
          isBusy = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region HighligtedText

    private string highlitedText;

    public string HighlitedText
    {
      get { return highlitedText; }
      set
      {
        if (value != highlitedText)
        {
          highlitedText = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

  
  }
}