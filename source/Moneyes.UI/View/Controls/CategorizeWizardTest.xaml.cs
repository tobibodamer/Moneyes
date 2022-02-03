using Moneyes.UI.ViewModels.Dialogs;
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

namespace Moneyes.UI.View.Controls
{
    /// <summary>
    /// Interaktionslogik für CategorizeWizardTest.xaml
    /// </summary>
    public partial class CategorizeWizardTest : UserControl
    {
        public CategorizeWizardTest()
        {
            InitializeComponent();
        }

        private void KeywordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key is Key.Return or Key.Enter)
            {
                var textBox = sender as TextBox;
                var value = textBox.Text;
                var vm = DataContext as CreateFilterViewModel;

                if (string.IsNullOrEmpty(value))
                {
                    e.Handled = true;
                    return;
                }

                if (vm.AddKeywordCommand.CanExecute(value))
                {
                    vm.AddKeywordCommand.Execute(value);
                }

                textBox.Clear();
                e.Handled = true;
            }
        }
    }
}
