using MudBlazor;

namespace Moneyes.UI.Blazor
{
    class MauiStatusMessageService : IStatusMessageService
    {
        ISnackbar _snackbar;
        public MauiStatusMessageService(ISnackbar snackbar)
        {
            _snackbar = snackbar;
        }
        public event Action<string, string, Action> NewMessage;
        public void ShowMessage(string messageText, string actionText = null, Action action = null)
        {
            NewMessage?.Invoke(messageText, actionText, action);
            _snackbar.Add(messageText, Severity.Normal, options =>
            {
                options.Action = actionText;
                options.Onclick = _ =>
                {
                    action();
                    return Task.CompletedTask;
                };
            });
            
        }
    }
}
