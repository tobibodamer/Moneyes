﻿using Moneyes.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Xaml.Behaviors;

namespace Moneyes.UI.View
{
    public class DialogBehavior : FrameworkElement
    {
        public IDialogViewModel ViewModel
        {
            get { return (IDialogViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ViewModel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(IDialogViewModel), typeof(DialogBehavior),
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

                if (!dialogBehavior.Dialog.BaseType.IsEquivalentTo(typeof(Window)))
                {
                    return;
                }

                var dlg = Activator.CreateInstance(dialogBehavior.Dialog) as Window;
                bool isClosed = false;

                dlg.Closed += (sender, args) =>
                {
                    isClosed = true;
                };

                dlg.DataContext = dialogBehavior.ViewModel;

                if (Application.Current.MainWindow != null
                    && Application.Current.MainWindow != dlg)
                {
                    dlg.Owner = Application.Current.MainWindow;
                }

                dialogBehavior.ViewModel.RequestClose += (sender, args) =>
                {
                    if (isClosed)
                    {
                        return;
                    }

                    dlg.DialogResult = args.Result;
                };

            _ = Task.Factory.StartNew(() =>
            {
                dlg.ShowDialog();
            }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
        }
    }
}
