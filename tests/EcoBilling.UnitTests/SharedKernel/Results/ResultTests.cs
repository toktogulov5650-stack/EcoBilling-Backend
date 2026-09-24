using EcoBilling.SharedKernel.Errors;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.UnitTests.SharedKernel.Results;

public sealed class ResultTests
{
    private static readonly Error FailureError =
        new("operation.failed", "The operation failed.", ErrorType.Failure);

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Same(Error.None, result.Error);
    }

    [Fact]
    public void Failure_HasTheProvidedError()
    {
        var result = Result.Failure(FailureError);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(FailureError, result.Error);
    }

    [Fact]
    public void Failure_RejectsNoError()
    {
        Assert.Throws<ArgumentException>(() => Result.Failure(Error.None));
    }

    [Fact]
    public void Failure_RejectsNullError()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
    }
}
