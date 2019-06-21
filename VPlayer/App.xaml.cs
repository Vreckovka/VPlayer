using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using VPlayer.Other;

namespace VPlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Custom startup to load IoC 
        /// </summary>
        /// <param name="e"></param>
        //protected override void OnStartup(StartupEventArgs e)
        //{
        //    base.OnStartup(e);

        //    //Setup IoC
        //    //IoC.Configure();

        //    //Show main window
        //    //Current.MainWindow = new MainWindow();
        //    Current.MainWindow.Show();
        //}
    }
}
