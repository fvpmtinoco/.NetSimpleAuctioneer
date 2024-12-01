namespace NetSimpleAuctioneer.API.Features.Shared
{
    public class SuccessOrError<T, TError> where TError : Enum
    {
        public bool HasErrors => Errors.Count > 0;
        public List<ErrorResult<TError>> Errors { get; } = [];
        public T Model { get; } = default!;

        public SuccessOrError(T model)
        {
            Model = model;
        }

        public SuccessOrError(params TError[] errors)
        {
            Errors.AddRange(errors.Select(x => new ErrorResult<TError>(x)));
        }

        public SuccessOrError(IEnumerable<TError> errors)
        {
            Errors.AddRange(errors.Select(x => new ErrorResult<TError>(x)));
        }

        public SuccessOrError(TError errorCode, params string[] invalidValues)
        {
            Errors.Add(new ErrorResult<TError>(errorCode, invalidValues));
        }

        public SuccessOrError(params ErrorResult<TError>[] errors)
        {
            Errors.AddRange(errors);
        }

        public SuccessOrError(IEnumerable<ErrorResult<TError>> errors)
        {
            Errors.AddRange(errors);
        }
    }

    public class VoidOrError<TError> where TError : Enum
    {
        public bool HasErrors => Errors.Count > 0;
        public List<ErrorResult<TError>> Errors { get; } = [];

        private VoidOrError() { }

        private VoidOrError(params TError[] errors)
        {
            Errors.AddRange(errors.Select(x => new ErrorResult<TError>(x)));
        }

        private VoidOrError(IEnumerable<TError> errors)
        {
            Errors.AddRange(errors.Select(x => new ErrorResult<TError>(x)));
        }

        private VoidOrError(params ErrorResult<TError>[] errors)
        {
            Errors.AddRange(errors);
        }

        private VoidOrError(IEnumerable<ErrorResult<TError>> errors)
        {
            Errors.AddRange(errors);
        }

        // Factory method for success
        public static VoidOrError<TError> Success() => new();

        // Factory method for failure with error codes
        public static VoidOrError<TError> Failure(params TError[] errors) =>
              new VoidOrError<TError>(errors);

        // Factory method for failure with ErrorResult instances
        public static VoidOrError<TError> Failure(params ErrorResult<TError>[] errors) =>
            new VoidOrError<TError>(errors);

        // Factory method for failure with IEnumerable of ErrorResult instances
        public static VoidOrError<TError> Failure(IEnumerable<ErrorResult<TError>> errors) =>
            new VoidOrError<TError>(errors);
    }
}
