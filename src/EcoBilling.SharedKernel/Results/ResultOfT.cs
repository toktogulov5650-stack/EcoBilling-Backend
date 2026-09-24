using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.SharedKernel.Results;

public sealed class Result<T> : Result
{
    private readonly T? value;

    private Result(T? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        this.value = value;
    }

    public T Value => IsSuccess
        ? value!
        : throw new InvalidOperationException("A failed result has no value.");

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new Result<T>(value, true, Error.None);
    }

    public new static Result<T> Failure(Error error) => new(default, false, error);
}
