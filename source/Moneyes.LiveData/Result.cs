using System;

namespace Moneyes.LiveData
{
    public class Result
    {
        public bool IsSuccessful { get; }

        protected Result(bool successful)
        {
            IsSuccessful = successful;
        }

        public static Result Successful()
        {
            return new(true);
        }

        public static Result<T> Successful<T>(T data)
        {
            return new(data: data);
        }

        public static Result Failed()
        {
            return new(false);
        }

        public static Result<T> Failed<T>()
        {
            return new(successful: false);
        }
    }

    public class Result<T> : Result
    {
        private readonly T _data;

        public T Data => !IsSuccessful
                    ? throw new InvalidOperationException("No data in error result.")
                    : _data;

        internal Result(bool successful = true, T data = default)
            : base(successful)
        {
            _data = data;
        }

        public static implicit operator Result<T>(T data)
        {
            return Successful(data);
        }
    }
}
