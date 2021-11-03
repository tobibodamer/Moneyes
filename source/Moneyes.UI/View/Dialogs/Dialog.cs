using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

namespace Moneyes.UI.View
{
    public class DialogBehavior : FrameworkElement
    {

        private INotifyPropertyChanged previous;

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

            new AddCategoryDialog()
            {
                DataContext = ((dynamic)DataContext).EditCategory
            }.ShowDialog();
        }



        public INotifyPropertyChanged ViewModel
        {
            get { return (INotifyPropertyChanged)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(INotifyPropertyChanged), typeof(DialogBehavior), 
                new PropertyMetadata(new PropertyChangedCallback(OnViewModelPropertyChanged)));

        public Type Dialog
        {
            get { return (Type)GetValue(DialogProperty); }
            set { SetValue(DialogProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DialogProperty =
            DependencyProperty.Register("Dialog", typeof(Type), typeof(DialogBehavior));



        private static void OnViewModelPropertyChanged(
            DependencyObject inDependencyObject, DependencyPropertyChangedEventArgs inEventArgs)
        {
            if (inDependencyObject is not DialogBehavior dialogBehavior)
            {
                return;
            }

            if (dialogBehavior.Dialog == null) { return; }

            if (dialogBehavior.ViewModel is null)
            {
                //dialogBehavior.Dialog.Close();
                return;
            }

            if (dialogBehavior.Dialog.BaseType.IsEquivalentTo(typeof(Window)))
            {
                var dlg = Activator.CreateInstance(dialogBehavior.Dialog) as Window;

                dlg.DataContext = dialogBehavior.ViewModel;
                dlg.ShowDialog();
            }
        }
    }
}
