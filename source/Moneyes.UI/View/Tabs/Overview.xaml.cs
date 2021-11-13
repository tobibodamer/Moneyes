using Moneyes.UI.ViewModels;
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
    /// Interaktionslogik für Overview.xaml
    /// </summary>
    public partial class Overview : UserControl
    {
        public Overview()
        {
            InitializeComponent();

            CategoryItems.SourceUpdated += CategoryItems_SourceUpdated;
            
        }

        private void CategoryItems_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(CategoryItems.ItemsSource);

            if (view == null) return;
            view.SortDescriptions.Add(new SortDescription("TotalExpense", ListSortDirection.Ascending));            
        }
    }
}
