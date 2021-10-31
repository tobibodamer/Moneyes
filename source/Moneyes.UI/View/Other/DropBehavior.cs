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

            uiElement.DragOver += (sender, args) =>
            {
                if (args.Data.GetFormats().Length == 0) { return; }

                object data = args.Data.GetData(args.Data.GetFormats()[0]);
                object target = (sender as FrameworkElement)?.DataContext;

                if (!(GetPreviewDropCommand(uiElement)?.CanExecute(data) ?? false))
                {
                    args.Effects = DragDropEffects.None;
                    args.Handled = true;
                }
            };

            uiElement.Drop += (sender, args) =>
            {
                if (args.Data.GetFormats().Length == 0) { return; }

                object data = args.Data.GetData(args.Data.GetFormats()[0]);
                object target = (sender as FrameworkElement)?.DataContext;

                if (GetPreviewDropCommand(uiElement)?.CanExecute(data) ?? false)
                {
                    GetPreviewDropCommand(uiElement)?.Execute(data);
                }

                args.Handled = true;
            };
        }
        
        #endregion
    }
}
