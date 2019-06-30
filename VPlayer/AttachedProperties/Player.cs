using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace VPlayer.AttachedProperties
{
    public class Player
    {
        public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.RegisterAttached(
            "IsPlaying",
            typeof(Boolean),
            typeof(Player),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static void SetIsPlaying(UIElement element, Boolean value)
        {
            element.SetValue(IsPlayingProperty, value);
        }

        public static Boolean GetIsPlaying(UIElement element)
        {
            return (Boolean) element.GetValue(IsPlayingProperty);
        }
    }
}
