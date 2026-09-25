using EcoBilling.Modules.Accounts.Domain;

namespace EcoBilling.UnitTests.Accounts.Domain;

public sealed class AccountNumberTests
{
    [Fact]
    public void Create_WithValue_NormalizesUsingTrimAndUpperInvariant()
    {
        var result = AccountNumber.Create("  ab-123  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("AB-123", result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingValue_ReturnsValidationError(string? value)
    {
        var result = AccountNumber.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(AccountErrors.InvalidAccountNumber, result.Error);
    }

    [Fact]
    public void Create_WithEquivalentValues_ProducesEqualNumbers()
    {
        var first = AccountNumber.Create("ab-123").Value;
        var second = AccountNumber.Create(" AB-123 ").Value;

        Assert.Equal(first, second);
    }
}
