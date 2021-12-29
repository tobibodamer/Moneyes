using Moneyes.UI.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Moneyes.UI.View
{
    /// <summary>
    /// Interaktionslogik für Overview.xaml
    /// </summary>
    public partial class Overview : UserControl
    {
        public Overview()
        {
            InitializeComponent();

            CategoryItems.SourceUpdated += CategoryItems_SourceUpdated;

            SwapCommand = new AsyncCommand<int[]>(async (indeces, ct) =>
            {
                (CategoryItems.ItemsSource as ObservableCollection<CategoryExpenseViewModel>)
                    .Move(indeces[0], indeces[1]);
            });
        }

        /// <summary>
        /// Command that swaps categories
        /// </summary>
        public ICommand SwapCommand { get; }

        private void CategoryItems_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(CategoryItems.ItemsSource);

            if (view == null) return;
            view.SortDescriptions.Add(new SortDescription("TotalExpense", ListSortDirection.Ascending));
        }
    }
}
