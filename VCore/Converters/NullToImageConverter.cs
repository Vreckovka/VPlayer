using System;
using System.Globalization;
using VCore.Controls;

namespace VCore.Converters
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
    }
}
