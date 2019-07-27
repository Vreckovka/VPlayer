using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Events;
using VPlayer.AudioStorage.Models;
using VPlayer.Core;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels;

namespace VPlayer.Library.ViewModels
{
    public class PlayableViewModel<TModel> : ViewModel<TModel>
    {
        public PlayableViewModel(TModel model) : base(model)
        {
            Play = new ActionCommand(OnPlay);
        }

        public bool IsPlaying { get; set; }

        public ICommand Play { get; set; }

        public virtual void OnPlay()
        {
            IsPlaying = true;
        }
    }
}
