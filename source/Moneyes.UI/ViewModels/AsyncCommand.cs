using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels
{
    internal class AsyncCommand : ICommand, IDisposable
    {
        private readonly Func<CancellationToken, Task> _execute;
        private readonly Func<bool> _canExecute;
        private readonly Action<Exception> _errorHandler;

        private CancellationTokenSource _cancellation = new();
        private Task _executingTask;

        public bool IsExecuting { get; private set; }

        public event EventHandler CanExecuteChanged;

        public AsyncCommand(Func<CancellationToken, Task> execute, Func<bool> canExecute = null, Action<Exception> errorHandler = null)
        {
            _execute = execute;
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }

        public bool CanExecute(object parameter)
        {
            return !IsExecuting && (_canExecute?.Invoke() ?? true);
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                // fire and forget
                ExecuteAsync(parameter).FireAndForgetSafeAsync(_errorHandler);
            }
        }

        public async Task ExecuteAsync(object parameter = null)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    IsExecuting = true;

                    OnCanExecuteChanged();

                    _executingTask = _execute?.Invoke(_cancellation.Token);

                    await _executingTask;
                }
                finally
                {
                    IsExecuting = false;
                }
            }

            OnCanExecuteChanged();
        }

        /// <summary>
        /// This method can be used to raise the CanExecuteChanged handler.
        /// This will force WPF to re-query the status of this command directly.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            _cancellation.Cancel();

            _cancellation = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _cancellation.Dispose();
            _executingTask.Dispose();
        }
    }

    internal class AsyncCommand<T> : ICommand, IDisposable where T : class
    {
        private readonly Func<T, CancellationToken, Task> _execute;
        private readonly Func<T, bool> _canExecute;
        private readonly Action<Exception> _errorHandler;

        private CancellationTokenSource _cancellation = new();
        private Task _executingTask;

        public bool IsExecuting { get; private set; }

        public event EventHandler CanExecuteChanged;

        public AsyncCommand(Func<T, CancellationToken, Task> execute, Func<T, bool> canExecute = null, Action<Exception> errorHandler = null)
        {
            _execute = execute;
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }

        public bool CanExecute(object parameter)
        {
            return !IsExecuting && (_canExecute?.Invoke(parameter as T) ?? true);
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                // fire and forget
                ExecuteAsync(parameter as T).FireAndForgetSafeAsync(_errorHandler);
            }
        }

        public async Task ExecuteAsync(T parameter = null)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    IsExecuting = true;

                    OnCanExecuteChanged();

                    _executingTask = _execute?.Invoke(parameter, _cancellation.Token);

                    await _executingTask;
                }
                finally
                {
                    IsExecuting = false;
                }
            }

            OnCanExecuteChanged();
        }

        public void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            _cancellation.Cancel();

            _cancellation = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _cancellation.Dispose();
            _executingTask.Dispose();
        }
    }
}
