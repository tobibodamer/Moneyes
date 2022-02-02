namespace Moneyes.LiveData
{
    public interface IOnlineBankingServiceFactory
    {
        IOnlineBankingService CreateService();
        IOnlineBankingService CreateService(OnlineBankingDetails bankingDetails);
    }
}