using System;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    internal static class TaskExtensions
    {
        public static async void FireAndForgetSafeAsync(this Task task, Action<Exception> errorHandler = null)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                errorHandler?.Invoke(ex);
            }
        }
    }
}
