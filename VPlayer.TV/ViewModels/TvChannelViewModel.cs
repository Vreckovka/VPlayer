using System;
using System.Threading.Tasks;
using IPTVStalker;
using VCore.Standard;
using VCore.Standard.ViewModels.TreeView;
using VPlayer.AudioStorage.DomainClasses.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  public class TvStalkerChannelViewModel : TvChannelViewModel
  {
    private readonly IPTVStalkerService stalkerService;

    public TvStalkerChannelViewModel(TvChannel model, IPTVStalkerService stalkerService) : base(model)
    {
      this.stalkerService = stalkerService ?? throw new ArgumentNullException(nameof(stalkerService));

      URL = null;
      Cmd = model.Url;
    }

    #region Cmd

    private string cmd;

    public string Cmd
    {
      get { return cmd; }
      set
      {
        if (value != cmd)
        {
          cmd = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    protected override async void OnSelected()
    {
      await Task.Run(() =>
      {
        URL = null;

        if (IsSelected && URL == null)
        {
          var link = stalkerService.GetLink(Cmd);

          if (link != null)
            URL = link.js.cmd.Substring("ffmpeg ".Length);
        }
      });
    }
  }

  public class TvChannelViewModel : TreeViewItemViewModel<TvChannel>, ISelectable
  {
    public TvChannelViewModel(TvChannel model) : base(model)
    {
      URL = Model.Url;
    }

    #region State

    private TVChannelState state;

    public TVChannelState State
    {
      get { return state; }
      set
      {
        if (value != state)
        {
          state = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region URL

    private string url;

    public string URL
    {
      get { return url; }
      set
      {
        if (value != url)
        {
          url = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region IsSelected

    private bool isSelected;

    public bool IsSelected
    {
      get { return isSelected; }
      set
      {
        if (value != isSelected)
        {
          isSelected = value;

          OnSelected();

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Name

    public string Name
    {
      get { return Model.Name; }
      set
      {
        if (value != Model.Name)
        {
          Model.Name = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public TvChannelViewModel Copy()
    {
      return new TvChannelViewModel(Model)
      {
        State = State,
        URL = URL
      };
    }


    protected virtual void OnSelected()
    {
    }

  }
}