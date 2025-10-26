using LinqToDB.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Infrastructure.Persistence.DataProvider;

namespace VideoConferencingApp.Infrastructure.Configuration.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly BaseDataProvider _baseDataProvider;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(BaseDataProvider baseDataProvider, ILogger<DatabaseHealthCheck> logger)
        {
            _baseDataProvider = baseDataProvider;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = _baseDataProvider.CreateDataConnection();

                var result = await connection.ExecuteAsync("SELECT 1");

                if (result > 0)
                {
                    return HealthCheckResult.Healthy("Database connection is healthy");
                }

                return HealthCheckResult.Unhealthy("Database connection failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("Database health check failed", ex);
            }
        }
    }
}
