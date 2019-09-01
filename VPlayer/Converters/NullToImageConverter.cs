using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MS.Internal.PresentationFramework;
using VPlayer.Core.Controls;
using Image = System.Windows.Controls.Image;

namespace VPlayer.Converters
{
    public class NullToImageConverter : BaseConverter
    {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PlayableWrapPanelItem playableWrapPanelItem)
            {
                if (playableWrapPanelItem.ImageThumbnail == null)
                {
                    return playableWrapPanelItem.DefaultImage;
                }

                return playableWrapPanelItem.ImageThumbnail;
            }
            else
            {
                throw new ArgumentException("Not valid value");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
