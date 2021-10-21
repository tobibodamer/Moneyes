using System;

namespace Moneyes.LiveData
{
    public static class ResultExtensions
    {
        public static T GetOrNull<T>(this Result<T> result)
        {
            if (result.IsSuccessful)
            {
                return result.Data;
            }

            return default;
        }

        public static T GetOrHandleError<T>(this Result<T> result, Action errorHandler)
        {
            if (!result.IsSuccessful)
            {
                errorHandler?.Invoke();
            }

            return result.Data;
        }

        public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> onSuccess)
        {
            if (result.IsSuccessful)
            {
                onSuccess?.Invoke(result.Data);
            }

            return result;
        }

        public static Result OnError(this Result result, Action onError)
        {
            if (!result.IsSuccessful)
            { 
                onError?.Invoke();
            }

            return result;
        }

        public static Result<T> OnError<T>(this Result<T> result, Action onError)
        {
            if (!result.IsSuccessful)
            {
                onError?.Invoke();
            }

            return result;
        }
    }
}
