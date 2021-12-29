namespace Moneyes.LiveData
{
    public interface IOnlineBankingServiceFactory
    {
        IOnlineBankingService CreateService(OnlineBankingDetails bankingDetails);
    }
}