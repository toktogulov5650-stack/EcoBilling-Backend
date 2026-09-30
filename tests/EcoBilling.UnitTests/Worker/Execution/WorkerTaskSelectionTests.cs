using EcoBilling.Worker.Execution;

namespace EcoBilling.UnitTests.Worker.Execution;

public sealed class WorkerTaskSelectionTests
{
    [Fact]
    public void GetTaskName_WithoutTaskOption_ReturnsNull()
    {
        var result = WorkerTaskSelection.GetTaskName([]);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("--task", "monthly-billing", "monthly-billing")]
    [InlineData("--task", "outbox", "outbox")]
    [InlineData("--task=monthly-billing", null, "monthly-billing")]
    public void GetTaskName_WithSupportedTask_ReturnsTask(
        string option,
        string? value,
        string expected)
    {
        var args = value is null ? [option] : new[] { option, value };

        var result = WorkerTaskSelection.GetTaskName(args);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("--task")]
    [InlineData("--task=unknown")]
    public void GetTaskName_WithInvalidTask_Throws(string option)
    {
        Assert.Throws<ArgumentException>(() => WorkerTaskSelection.GetTaskName([option]));
    }
}
