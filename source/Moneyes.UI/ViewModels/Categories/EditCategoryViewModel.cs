using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;
using Moneyes.UI.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    internal class EditCategoryViewModel : CategoryViewModel, IDialogViewModel
    {
        public ICommand ApplyCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public event EventHandler<RequestCloseDialogEventArgs> RequestClose;

        public EditCategoryViewModel(ICategoryService categoryService, IStatusMessageService statusMessageService) 
            : base(categoryService, statusMessageService)
        {
            ApplyCommand = SaveCommand;

            Saved += (category) =>
            {
                RequestClose?.Invoke(this, new() { Result = true });
            };

            CancelCommand = new AsyncCommand(async ct =>
            {
                RequestClose?.Invoke(this, new() { Result = false });
            });
        }


    }
}
