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
using Moneyes.Core;
using System.Windows.Controls.Primitives;
using System.Collections;

namespace Moneyes.UI.View
{
    /// <summary>
    /// Interaktionslogik für TransactionsListView.xaml
    /// </summary>
    public partial class TransactionsControl : UserControl
    {
        public TransactionsControl()
        {
            InitializeComponent();

            _ = new ListViewDragDropManager<TransactionViewModel>(transactionsListView);
        }

        private void ItemContextMenu_Opened(object sender, RoutedEventArgs e)
        {            
            ContextMenu menu = sender as ContextMenu;
            
            var addToCategoryItem = menu.Items.Cast<MenuItem>().First(x => x.Name == "AddToCategoryItem");

            if (addToCategoryItem == null)
            {
                return;
            }

            TransactionsViewModel transactionsViewModel = (DataContext as TransactionsViewModel);

            if (transactionsViewModel is null)
            {
                return;
            }

            if (transactionsListView.SelectedItems.Count == 0)
            {
                addToCategoryItem.Visibility = Visibility.Collapsed;
                return;
            }

            addToCategoryItem.Visibility = Visibility.Visible;
            addToCategoryItem.Items.Clear();

            addToCategoryItem.Items.Add(new MenuItem()
            {
                Header = "New category...",
                Command = transactionsViewModel.AddToNewCategory
            });

            addToCategoryItem.Items.Add(new Separator());

            foreach (var c in transactionsViewModel.Categories)
            {
                addToCategoryItem.Items.Add(new MenuItem()
                {
                    Header = c.Name,
                    IsCheckable = true,
                    IsChecked = transactionsListView.SelectedItems.Cast<TransactionViewModel>().All(x => x.Transaction.Category?.Id == c.Id),
                    Command = transactionsViewModel.AddToCategory,
                    CommandParameter = c
                });
            }
        }        
    }
}
