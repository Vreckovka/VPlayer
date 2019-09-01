using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace VPlayer.Core.Controls
{
    public class Finder : Control
    {
        #region KeyDownCommand

        public ICommand KeyDownCommand
        {
            get { return (ICommand)GetValue(KeyDownCommandProperty); }
            set { SetValue(KeyDownCommandProperty, value); }
        }

        public static readonly DependencyProperty KeyDownCommandProperty =
            DependencyProperty.Register(
                nameof(KeyDownCommand),
                typeof(ICommand),
                typeof(Finder),
                new PropertyMetadata(null));


        #endregion
    }
}
