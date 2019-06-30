using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TestingWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Class
        {
            public Class()
            {
               
            }
            public string Name { get; set; }
            public string Artist { get; set; }
        }

        public List<Class> List { get; set; } = new List<Class>
        {
            new Class()
            {
                Artist = "Jozko",
                Name = "BEST SONG EVER"
            },
            new Class()
            {
                Artist = "Zdeno",
                Name = "BEST SONG EVER1"
            },
            new Class()
            {
                Artist = "Zdeno",
                Name = "BEST SONG EVER2"
            }


        };


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
