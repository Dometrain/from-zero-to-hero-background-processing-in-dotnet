using APIProject.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace APIProject.HealthChecks
{
    public class WorkerHeartBeatHealthCheck : IHealthCheck
    {
        private readonly WorkerHeartbeatService _workerHeartbeatService;

        public WorkerHeartBeatHealthCheck(WorkerHeartbeatService workerHeartbeatService)
        {
            _workerHeartbeatService = workerHeartbeatService;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var lastHeartbeat = _workerHeartbeatService.GetLastHeartBeat();
            var heartbeatThreshold = TimeSpan.FromSeconds(30); // Define a threshold for considering the worker healthy

            if (lastHeartbeat is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("No heartbeat recorded yet"));
            }

            var timeSinceLastHeartbeat = DateTime.UtcNow - lastHeartbeat.Value;

            if (timeSinceLastHeartbeat <= heartbeatThreshold)
            {
                return Task.FromResult(HealthCheckResult.Healthy($"Last heartbeat was {timeSinceLastHeartbeat.TotalSeconds} seconds ago"));
            }
            else
            {
                return Task.FromResult(HealthCheckResult.Unhealthy($"Last heartbeat was {timeSinceLastHeartbeat.TotalSeconds} seconds ago, which exceeds the threshold of {heartbeatThreshold.TotalSeconds} seconds"));
            }
        }
    }
}
