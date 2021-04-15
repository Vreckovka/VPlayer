using System;
using System.Threading.Tasks;
using IPTVStalker;
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
    }

  
    #region OnSelected

    protected override async void OnSelected(bool isSelected)
    {
      await Task.Run(() =>
      {
        URL = null;

        if (isSelected && URL == null && Model != null)
        {
          var link = stalkerService.GetLink(Model.Url);

          if (link != null)
            URL = link.js.cmd.Substring("ffmpeg ".Length);
        }
      });
    }

    #endregion
  }
}