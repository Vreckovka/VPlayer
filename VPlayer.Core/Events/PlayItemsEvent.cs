using Prism.Events;
using VPlayer.Core.ViewModels;

namespace VPlayer.Core.Events
{
  public class PlayItemsEvent<TModel,TItemViewModel> : PubSubEvent<PlayItemsEventData<TItemViewModel>> 
    where TItemViewModel : IItemInPlayList<TModel>
    where  TModel : IPlayableModel
  {

  }
}