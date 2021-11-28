using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    public class RelayCommand : ICommand, INotifyPropertyChanged
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;
        private readonly Action<Exception> _errorHandler;

        private bool _isExecuting;
        public bool IsExecuting
        {
            get => _isExecuting;
            private set
            {
                _isExecuting = value;
                OnPropertyChanged();
            }
        }

        public event EventHandler CanExecuteChanged
        {
            add
            {
                CommandManager.RequerySuggested += value;
                CanExecuteChangedInternal += value;
            }
            remove
            {
                CommandManager.RequerySuggested -= value;
                CanExecuteChangedInternal -= value;
            }
        }

        protected event EventHandler CanExecuteChangedInternal;

        public event PropertyChangedEventHandler PropertyChanged;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null, Action<Exception> errorHandler = null)
        {
            _execute = execute;
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }
        public RelayCommand(Action execute, Func<bool> canExecute = null, Action<Exception> errorHandler = null)
            : this(param => execute(), param => canExecute(), errorHandler)
        {
        }

        public bool CanExecute(object parameter)
        {
            return !IsExecuting && (_canExecute?.Invoke(parameter) ?? true);
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    IsExecuting = true;
                    OnCanExecuteChanged();

                    _execute?.Invoke(parameter);
                }
                catch (Exception ex)
                {
                    if (_errorHandler == null)
                    {
                        throw;
                    }

                    _errorHandler(ex);
                }
                finally
                {
                    IsExecuting = false;
                    OnCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// This method can be used to raise the CanExecuteChanged handler.
        /// This will force WPF to re-query the status of this command directly.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        protected void OnCanExecuteChanged()
        {
            CanExecuteChangedInternal?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }
    }

    public class RelayCommand<TParam> : RelayCommand
        where TParam : class
    {
        public RelayCommand(Action<TParam> execute, Func<TParam, bool> canExecute = null, Action<Exception> errorHandler = null)
            : base(param => execute(param as TParam),
                  canExecute != null ? param => canExecute(param as TParam) : null,
                  errorHandler)
        {
        }
    }

    public class CollectionRelayCommand<TParam> : RelayCommand<IEnumerable>
        where TParam : class
    {
        public CollectionRelayCommand(Action<IEnumerable<TParam>> execute, Func<IEnumerable<TParam>, bool> canExecute = null,
            Action<Exception> errorHandler = null)
            : base(param => execute(param?.Cast<TParam>()),
                  canExecute != null ? param => canExecute(param?.Cast<TParam>()) : null,
                  errorHandler)
        {
        }
    }
}
