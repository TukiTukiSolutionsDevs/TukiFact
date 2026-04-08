using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using TukiFact.Application.Interfaces;

namespace TukiFact.Infrastructure.Services;

public class MinioStorageService : IStorageService
{
    private readonly IMinioClient _minioClient;
    private const string XmlBucket = "tukifact-xml";
    private const string CdrBucket = "tukifact-cdr";
    private const string PdfBucket = "tukifact-pdf";

    public MinioStorageService(IConfiguration configuration)
    {
        var endpoint = configuration["MinIO:Endpoint"] ?? "localhost:9000";
        var accessKey = configuration["MinIO:AccessKey"] ?? "tukifact_minio";
        var secretKey = configuration["MinIO:SecretKey"] ?? "tukifact_minio_2026";

        _minioClient = new MinioClient()
            .WithEndpoint(endpoint)
            .WithCredentials(accessKey, secretKey)
            .Build();
    }

    public Task<string> UploadXmlAsync(Guid tenantId, string fileName, byte[] content, CancellationToken ct = default)
        => UploadAsync(XmlBucket, $"{tenantId}/{fileName}", content, "application/xml", ct);

    public Task<string> UploadCdrAsync(Guid tenantId, string fileName, byte[] content, CancellationToken ct = default)
        => UploadAsync(CdrBucket, $"{tenantId}/{fileName}", content, "application/zip", ct);

    public Task<string> UploadPdfAsync(Guid tenantId, string fileName, byte[] content, CancellationToken ct = default)
        => UploadAsync(PdfBucket, $"{tenantId}/{fileName}", content, "application/pdf", ct);

    public async Task<byte[]?> DownloadAsync(string bucketName, string objectName, CancellationToken ct = default)
    {
        try
        {
            using var ms = new MemoryStream();
            await _minioClient.GetObjectAsync(new GetObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream => stream.CopyTo(ms)), ct);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> UploadAsync(string bucket, string objectName, byte[] content, string contentType, CancellationToken ct)
    {
        using var ms = new MemoryStream(content);
        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectName)
            .WithStreamData(ms)
            .WithObjectSize(content.Length)
            .WithContentType(contentType), ct);

        return $"{bucket}/{objectName}";
    }
}
