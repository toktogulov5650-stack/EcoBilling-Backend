namespace EcoBilling.Worker.Execution;

public static class WorkerTaskSelection
{
    public static string? GetTaskName(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            if (string.Equals(argument, "--task", StringComparison.Ordinal))
            {
                if (index + 1 >= args.Count || string.IsNullOrWhiteSpace(args[index + 1]))
                {
                    throw new ArgumentException(
                        "Worker option '--task' requires a task name.",
                        nameof(args));
                }

                return Validate(args[index + 1]);
            }

            const string prefix = "--task=";
            if (argument.StartsWith(prefix, StringComparison.Ordinal))
            {
                return Validate(argument[prefix.Length..]);
            }
        }

        return null;
    }

    private static string Validate(string taskName) =>
        taskName switch
        {
            "monthly-billing" or "outbox" => taskName,
            _ => throw new ArgumentException(
                $"Unknown worker task '{taskName}'. Supported tasks: monthly-billing, outbox.",
                nameof(taskName))
        };
}
