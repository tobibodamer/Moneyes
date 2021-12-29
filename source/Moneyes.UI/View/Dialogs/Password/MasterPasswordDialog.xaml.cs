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
using System.Windows.Shapes;

namespace Moneyes.UI.View
{
    /// <summary>
    /// Interaktionslogik für InitMasterPasswordDialog.xaml
    /// </summary>
    public partial class MasterPasswordDialog : Window
    {
        public MasterPasswordDialog()
        {
            InitializeComponent();

            txtPasswordResponse.Focus();

            DataContextChanged += InitMasterPasswordDialog_DataContextChanged;
        }

        private void InitMasterPasswordDialog_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is IDialogViewModel dialogViewModel)
            {
                dialogViewModel.RequestClose += DialogViewModel_RequestClose;
            }
            else if (e.OldValue is IDialogViewModel oldDialogVM)
            {
                oldDialogVM.RequestClose -= DialogViewModel_RequestClose;
            }
        }

        private void DialogViewModel_RequestClose(object sender, RequestCloseDialogEventArgs e)
        {
            DialogResult = e.Result;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
