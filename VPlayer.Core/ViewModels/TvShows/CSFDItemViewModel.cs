using System.Diagnostics;
using System.Windows.Input;
using VCore.Standard;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.Scrappers.CSFD.Domain;

namespace VPlayer.Core.ViewModels.TvShows
{
  public class CSFDItemViewModel : ViewModel<CSFDItem>
  {
    public CSFDItemViewModel(CSFDItem model) : base(model)
    {
    }

    #region OpenCsfd

    private ActionCommand openCsfd;
    public ICommand OpenCsfd
    {
      get
      {
        return openCsfd ??= new ActionCommand(OnOpenCsfd);
      }
    }

    private void OnOpenCsfd()
    {
      if (!string.IsNullOrEmpty(Model.Url))
      {
        Process.Start(new ProcessStartInfo()
        {
          FileName = Model.Url,
          UseShellExecute = true,
          Verb = "open"
        });
      }
    }

    #endregion
  }
}