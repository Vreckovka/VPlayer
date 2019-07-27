using KeyListener;
using Prism.Events;
using VPlayer.Core;
using VPlayer.Core.ViewModels;

namespace VPlayer.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        public string Title { get; set; }

        public MainWindowViewModel(IEventAggregator eventAggregator)
        {
            Title = "VPlayer";

            PlayerHandler playerHandler = new PlayerHandler(eventAggregator);
        }
    }
}
