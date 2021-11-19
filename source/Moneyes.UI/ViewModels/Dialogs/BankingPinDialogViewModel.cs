namespace Moneyes.UI.ViewModels
{
    class BankingPinDialogViewModel : PasswordDialogViewModel
    {
        public BankingPinDialogViewModel()
        {
            Title = "Password required";
            Text = "Enter your online banking password / PIN:";
        }

        public bool SavePassword { get; set; }
    }
}
