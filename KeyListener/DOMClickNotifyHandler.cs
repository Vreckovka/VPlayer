using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Windows.Foundation.Metadata;

namespace Listener
{
    public delegate void NotifyAppHandler(string keyComb);

    /// <summary>
    /// Sample native object for injecting to the WebView.
    /// </summary>
    //[AllowForWeb]
    public sealed class DOMClickNotifyHandler
    {
        public event NotifyAppHandler NotifyAppEvent;

        public void setKeyCombination(string keyPress)
        {
            OnNotifyAppEvent(keyPress);
        }

        private void OnNotifyAppEvent(string keycomb)
        {
            NotifyAppEvent?.Invoke(keycomb);
        }
    }
}
