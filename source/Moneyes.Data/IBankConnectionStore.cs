using Moneyes.LiveData;

namespace Moneyes.Data
{
    public interface IBankConnectionStore
    {
        bool HasBankingDetails { get; }

        OnlineBankingDetails GetBankingDetails();
        bool SetBankingDetails(OnlineBankingDetails bankingDetails);
    }
}