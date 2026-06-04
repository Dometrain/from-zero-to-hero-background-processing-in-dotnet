namespace APIProject.Services
{
    public class WorkerHeartbeatService
    {
        private readonly object _lock = new();

        public DateTimeOffset? LastHeartBeat { get; private set; }

        public void Beat()
        {
            lock (_lock)
            {
                LastHeartBeat = DateTimeOffset.UtcNow;
            }
        }

        public DateTimeOffset? GetLastHeartBeat()
        {
            lock(_lock)
            {
                return LastHeartBeat;
            }
        }
    }
}
