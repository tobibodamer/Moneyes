using Moneyes.UI.ViewModels;
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
        public MainWindow(MainWindowViewModel dataContext)
        {
            InitializeComponent();

            DataContext = dataContext;

            CloseButton.Click += (s, args) =>
            {
                this.Close();
            };

            Snackbar.MessageQueue = new(TimeSpan.FromSeconds(5));

            dataContext.NewStatusMessage += (message, actionText, action) =>
            {
                if (actionText == null || action == null)
                {
                    Snackbar.MessageQueue.Enqueue(message);
                }
                else
                {
                    Snackbar.MessageQueue.Enqueue(message, actionText, action);
                }
            };
        }

        private void Move(object sender, MouseButtonEventArgs e)
        {
            if (CloseButton.IsMouseOver) { return; }
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        bool _shown;

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_shown)
            {
                return;
            }

            _shown = true;

            (DataContext as MainWindowViewModel)?.LoadedCommand?.Execute(null);

            // Your code here.
        }
    }
}
