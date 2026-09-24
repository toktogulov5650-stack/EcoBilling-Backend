using EcoBilling.SharedKernel.Errors;
using EcoBilling.SharedKernel.Results;

namespace EcoBilling.UnitTests.SharedKernel.Results;

public sealed class ResultOfTTests
{
    private static readonly Error FailureError =
        new("account.not_found", "The account was not found.", ErrorType.NotFound);

    [Fact]
    public void Success_HasValueAndNoError()
    {
        var result = Result<string>.Success("value");

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal("value", result.Value);
        Assert.Same(Error.None, result.Error);
    }

    [Fact]
    public void Success_AcceptsValueTypeDefault()
    {
        var result = Result<int>.Success(0);

        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void Success_RejectsNullValue()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }

    [Fact]
    public void Failure_HasError()
    {
        var result = Result<string>.Failure(FailureError);

        Assert.True(result.IsFailure);
        Assert.Same(FailureError, result.Error);
    }

    [Fact]
    public void Failure_ValueCannotBeRead()
    {
        var result = Result<string>.Failure(FailureError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_RejectsNoError()
    {
        Assert.Throws<ArgumentException>(() => Result<string>.Failure(Error.None));
    }

    [Fact]
    public void Failure_RejectsNullError()
    {
        Assert.Throws<ArgumentNullException>(() => Result<string>.Failure(null!));
    }
}
