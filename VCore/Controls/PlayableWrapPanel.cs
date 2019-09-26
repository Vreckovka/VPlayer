using System.Collections;
using System.Windows.Controls;

namespace VCore.Controls
{
    public class PlayableWrapPanel : ItemsControl
    {
    protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
    {
      base.OnItemsSourceChanged(oldValue, newValue);
    }
  }
}
