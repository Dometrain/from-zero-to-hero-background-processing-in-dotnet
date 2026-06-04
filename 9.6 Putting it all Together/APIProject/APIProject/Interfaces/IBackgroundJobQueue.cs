using APIProject.Models;

namespace APIProject.Interfaces
{
    public interface IBackgroundJobQueue
    {
        ValueTask QueueAsync(BackgroundJob job, CancellationToken cancellationToken = default);

        ValueTask<BackgroundJob?> DequeueAsync(CancellationToken cancellationToken = default);
    }
}
