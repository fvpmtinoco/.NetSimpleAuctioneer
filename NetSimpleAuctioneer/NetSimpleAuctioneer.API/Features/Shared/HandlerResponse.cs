namespace NetSimpleAuctioneer.API.Features.Shared
{
    public class SuccessOrError<T, TError> where TError : struct, Enum
    {
        public bool HasError => Error != null;
        public TError? Error { get; }
        public T Result { get; } = default!;

        // Private constructors to prevent direct instantiation
        private SuccessOrError(T result)
        {
            Result = result;
            Error = null!;
        }

        private SuccessOrError(TError errorCode)
        {
            Error = errorCode;
        }

        // Factory method for success with result
        public static SuccessOrError<T, TError> Success(T model) => new SuccessOrError<T, TError>(model);

        // Factory method for failure with error
        public static SuccessOrError<T, TError> Failure(TError errorCode) =>
            new SuccessOrError<T, TError>(errorCode);
    }

    public class VoidOrError<TError> where TError : struct, Enum
    {
        public bool HasError => Error != null;
        public TError? Error { get; }

        private VoidOrError() => Error = null!;

        private VoidOrError(TError error)
        {
            Error = error;
        }

        // Factory method for success
        public static VoidOrError<TError> Success() => new();

        // Factory method for failure with error codes
        public static VoidOrError<TError> Failure(TError error) =>
              new VoidOrError<TError>(error);
    }
}
