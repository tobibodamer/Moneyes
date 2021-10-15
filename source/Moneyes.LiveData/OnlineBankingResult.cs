namespace Moneyes.LiveData
{
    public class OnlineBankingResult<T>
    {
        public bool IsSuccessful { get; init; }
        public T Data { get; init; }

        public OnlineBankingResult(T data)
        {
            Data = data;
            IsSuccessful = true;
        }

        public OnlineBankingResult(bool isSuccessful)
        {
            IsSuccessful = isSuccessful;
        }
    }
}
