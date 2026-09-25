namespace EcoBilling.Worker.Execution;

public interface IWorkerTask
{
    string Name { get; }

    Task ExecuteAsync(CancellationToken cancellationToken);
}
