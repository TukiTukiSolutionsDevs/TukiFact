namespace TukiFact.Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadXmlAsync(Guid tenantId, string fileName, byte[] content, CancellationToken ct = default);
    Task<string> UploadCdrAsync(Guid tenantId, string fileName, byte[] content, CancellationToken ct = default);
    Task<string> UploadPdfAsync(Guid tenantId, string fileName, byte[] content, CancellationToken ct = default);
    Task<byte[]?> DownloadAsync(string bucketName, string objectName, CancellationToken ct = default);
}
