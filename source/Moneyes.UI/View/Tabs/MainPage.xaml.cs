using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaktionslogik für MainPage.xaml
    /// </summary>
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
            new ListViewDragDropManager<Moneyes.Core.Transaction>(transactionsListView);

            previous = (INotifyPropertyChanged)this.DataContext;
            DataContextChanged += (sender, args) => SubscribeToFooChanges((INotifyPropertyChanged)args.NewValue);
            SubscribeToFooChanges(previous);
        }

        INotifyPropertyChanged previous;

        // subscriber
        private void SubscribeToFooChanges(INotifyPropertyChanged viewModel)
        {
            if (previous != null)
                previous.PropertyChanged -= FooChanged;
            previous = viewModel;
            if (viewModel != null)
                viewModel.PropertyChanged += FooChanged;
        }

        // event handler
        private void FooChanged(object sender, PropertyChangedEventArgs args)
        {
            if (!args.PropertyName.Equals("EditCategory"))
                return;

            //new AddCategoryDialog()
            //{
            //    DataContext = ((dynamic)DataContext).EditCategory
            //}.ShowDialog();
        }

        private void TreeView_Drop(object sender, DragEventArgs e)
        {
            var dataContext = (e.OriginalSource as FrameworkElement)?.DataContext;

            if (e.Data != null)
            {
                var data = e.Data.GetData(e.Data.GetFormats()[0]);

                ((dynamic)DataContext).AddToCategoryCommand?.Execute((data, dataContext));
            }
        }
    }
}
