using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;

namespace TukiFact.Api.Middleware;

public class MinioHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;

    public MinioHealthCheck(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoint = _configuration.GetValue<string>("MinIO:Endpoint") ?? "localhost:9000";
            var accessKey = _configuration.GetValue<string>("MinIO:AccessKey") ?? "tukifact_minio";
            var secretKey = _configuration.GetValue<string>("MinIO:SecretKey") ?? "tukifact_minio_2026";

            var minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .Build();

            // List buckets as a connectivity health check
            var result = await minioClient.ListBucketsAsync(cancellationToken);
            var bucketCount = result?.Buckets?.Count ?? 0;

            return HealthCheckResult.Healthy($"MinIO is reachable. Buckets: {bucketCount}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("MinIO is unreachable", ex);
        }
    }
}
