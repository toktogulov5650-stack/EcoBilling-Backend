using EcoBilling.SharedKernel.Errors;

namespace EcoBilling.UnitTests.SharedKernel.Errors;

public sealed class ErrorTests
{
    [Fact]
    public void None_HasOnlyTheNoneTypeAndNoClientFacingContent()
    {
        Assert.Equal(ErrorType.None, Error.None.Type);
        Assert.Empty(Error.None.Code);
        Assert.Empty(Error.None.Description);
        Assert.True(Error.None.IsNone);
    }

    [Fact]
    public void Constructor_CreatesTypedErrorAndTrimsText()
    {
        var error = new Error(" resident.not_found ", " Resident was not found. ", ErrorType.NotFound);

        Assert.Equal("resident.not_found", error.Code);
        Assert.Equal("Resident was not found.", error.Description);
        Assert.Equal(ErrorType.NotFound, error.Type);
        Assert.False(error.IsNone);
    }

    [Fact]
    public void Constructor_RejectsNullCode()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Error(null!, "Description", ErrorType.Failure));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyCode(string code)
    {
        Assert.Throws<ArgumentException>(
            () => new Error(code, "Description", ErrorType.Failure));
    }

    [Fact]
    public void Constructor_RejectsNullDescription()
    {
        Assert.Throws<ArgumentNullException>(
            () => new Error("error.code", null!, ErrorType.Failure));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_RejectsEmptyDescription(string description)
    {
        Assert.Throws<ArgumentException>(
            () => new Error("error.code", description, ErrorType.Failure));
    }

    [Fact]
    public void Constructor_RejectsNoneType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Error("error.code", "Description", ErrorType.None));
    }

    [Fact]
    public void Constructor_RejectsUnknownType()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Error("error.code", "Description", (ErrorType)999));
    }

    [Fact]
    public void Errors_WithTheSameData_AreEqual()
    {
        var first = new Error("error.code", "Description", ErrorType.Conflict);
        var second = new Error("error.code", "Description", ErrorType.Conflict);

        Assert.Equal(first, second);
    }
}
