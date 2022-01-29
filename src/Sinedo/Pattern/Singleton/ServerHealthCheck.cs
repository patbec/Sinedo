using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Sinedo.Singleton
{
    public class ServerHealthCheck : IHealthCheck
    {
        private readonly DownloadRepository repository;

        public ServerHealthCheck(DownloadRepository repository)
        {
            this.repository = repository;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            // Versuchen Schreibzugriff auf die Download-Verwaltung zu erlangen.
            Task task = repository.Context.WriterLockAsync(cancellationToken);

            // Kann der Aufruf nicht innerhalb von 5 Sekunden ausgeführt werden, ist ein DeadLock aufgetreten.
            bool canAccessStatePattern = task.Wait(5000, cancellationToken);


            if (cancellationToken.IsCancellationRequested || canAccessStatePattern)
            {
                return Task.FromResult(
                       HealthCheckResult.Healthy("Application works normal."));
            }

            return Task.FromResult(
                   HealthCheckResult.Unhealthy("Application is frozen."));
        }
    }
}