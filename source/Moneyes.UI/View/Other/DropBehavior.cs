using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Moneyes.UI.View
{
    /// <summary>
    /// This is an Attached Behavior and is intended for use with
    /// XAML objects to enable binding a drag and drop event to
    /// an ICommand.
    /// </summary>
    public class DropBehavior
    {
        public static ICommand GetPreviewDropCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(PreviewDropCommandProperty);
        }

        public static void SetPreviewDropCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(PreviewDropCommandProperty, value);
        }

        public static readonly DependencyProperty PreviewDropCommandProperty =
            DependencyProperty.RegisterAttached("PreviewDropCommand", typeof(ICommand), typeof(DropBehavior),
                new PropertyMetadata(OnPreviewDropCommandPropertyChanged));



        public static ICommand GetPreviewDropCopyCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(PreviewDropCopyCommandProperty);
        }

        public static void SetPreviewDropCopyCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(PreviewDropCopyCommandProperty, value);
        }

        // Using a DependencyProperty as the backing store for PreviewDropCopyCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreviewDropCopyCommandProperty =
            DependencyProperty.RegisterAttached("PreviewDropCopyCommand", typeof(ICommand), typeof(DropBehavior),
                new PropertyMetadata(OnPreviewDropCommandPropertyChanged));



        #region The PropertyChangedCallBack method
        /// <summary>
        /// The OnCommandChanged method. This event handles the initial binding and future
        /// binding changes to the bound ICommand
        /// </summary>
        /// <param name="inDependencyObject">A DependencyObject</param>
        /// <param name="inEventArgs">A DependencyPropertyChangedEventArgs object.</param>
        private static void OnPreviewDropCommandPropertyChanged(
            DependencyObject inDependencyObject, DependencyPropertyChangedEventArgs inEventArgs)
        {
            if (inDependencyObject is not UIElement uiElement)
            {
                return;
            }

            if (inEventArgs.NewValue == null && inEventArgs.OldValue != null)
            {
                uiElement.DragOver -= UiElement_DragOver;
                uiElement.Drop -= UiElement_Drop;

                return;
            }

            uiElement.DragOver += UiElement_DragOver;

            uiElement.Drop += UiElement_Drop;
        }

        private static void UiElement_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetFormats().Length == 0) { return; }
            if (sender is not FrameworkElement frameworkElement) { return; }

            object data = e.Data.GetData(e.Data.GetFormats()[0]);

            if (!e.KeyStates.HasFlag(DragDropKeyStates.ControlKey))
            {
                if (GetPreviewDropCommand(frameworkElement)?.CanExecute(data) ?? false)
                {
                    GetPreviewDropCommand(frameworkElement)?.Execute(data);
                }
            }
            else
            {
                if (GetPreviewDropCopyCommand(frameworkElement)?.CanExecute(data) ?? false)
                {
                    GetPreviewDropCopyCommand(frameworkElement)?.Execute(data);
                }
            }

            e.Handled = true;
        }

        private static void UiElement_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetFormats().Length == 0)
            {
                e.Effects = DragDropEffects.None;
                return;
            }
            if (sender is not FrameworkElement frameworkElement) { return; }

            object data = e.Data.GetData(e.Data.GetFormats()[0]);

            if (!e.KeyStates.HasFlag(DragDropKeyStates.ControlKey))
            {
                ICommand previewDropCommand = GetPreviewDropCommand(frameworkElement);
                bool canExecute = previewDropCommand?.CanExecute(data) ?? false;

                if (!canExecute)
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                    
                    return;
                }

                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
            else
            {
                ICommand previewDropCommand = GetPreviewDropCopyCommand(frameworkElement);
                bool canExecute = previewDropCommand?.CanExecute(data) ?? false;

                if (!canExecute)
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                    return;
                }

                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        #endregion
    }
}
