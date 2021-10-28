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

namespace Moneyes.UI.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(object dataContext)
        {
            InitializeComponent();

            DataContext = dataContext;

            CloseButton.Click += (s, args) =>
            {
                this.Close();
            };
        }

        private void Move(object sender, MouseButtonEventArgs e)
        {
            if (CloseButton.IsMouseOver) { return; }
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }
    }
}
