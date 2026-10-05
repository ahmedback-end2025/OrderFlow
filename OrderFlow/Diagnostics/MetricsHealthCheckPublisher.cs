using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OrderFlow.Application.Diagnostics;

namespace OrderFlow.Diagnostics
{
    public sealed class MetricsHealthCheckPublisher : IHealthCheckPublisher
    {

        private readonly ILogger<MetricsHealthCheckPublisher> _logger;

        public MetricsHealthCheckPublisher(ILogger<MetricsHealthCheckPublisher> logger)
            => _logger = logger;

        public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            foreach (var (name, entry) in report.Entries)
            {
                var healthy = entry.Status == HealthStatus.Healthy ? 1 : 0;
                var previous = OrderFlowDiagnostics.DependencyHealth.GetOrAdd(name, healthy);

                if (previous != healthy)
                {
                    _logger.LogWarning("Dependency {Dependency} health changed: {OldStatus} -> {NewStatus}",
                        name, previous == 1 ? "Healthy" : "Unhealthy", entry.Status);
                }

                OrderFlowDiagnostics.DependencyHealth[name] = healthy;
            }

            return Task.CompletedTask;
        }
    }
}
