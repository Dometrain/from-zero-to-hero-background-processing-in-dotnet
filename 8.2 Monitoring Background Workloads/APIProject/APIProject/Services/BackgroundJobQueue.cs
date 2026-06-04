using APIProject.Interfaces;
using APIProject.Models;
using System.Threading.Channels;

namespace APIProject.Services
{
    public class BackgroundJobQueue : IBackgroundJobQueue
    {
        private readonly Channel<BackgroundJob> _queue;
        
        public BackgroundJobQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };

            _queue = Channel.CreateBounded<BackgroundJob>(options);
        }

        public async ValueTask<BackgroundJob?> DequeueAsync(CancellationToken cancellationToken = default)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }

        public async ValueTask QueueAsync(BackgroundJob job, CancellationToken cancellationToken = default)
        {
            await _queue.Writer.WriteAsync(job, cancellationToken);
        }
    }
}
