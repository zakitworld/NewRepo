namespace OnlineVoting_and_Ticketing_app.Core
{
    /// <summary>
    /// Represents the outcome of an operation that returns a value.
    /// Avoids silent null returns or swallowed exceptions across service boundaries.
    /// </summary>
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public T? Value { get; }
        public string? Error { get; }

        private Result(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        public static Result<T> Success(T value) => new(true, value, null);
        public static Result<T> Failure(string error) => new(false, default, error);

        /// <summary>Allows implicit wrapping: return myValue; becomes return Result.Success(myValue);</summary>
        public static implicit operator Result<T>(T value) => Success(value);
    }

    /// <summary>
    /// Represents the outcome of an operation that returns no value.
    /// </summary>
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string? Error { get; }

        private Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new(true, null);
        public static Result Failure(string error) => new(false, error);
    }
}
