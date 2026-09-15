namespace TripSplit.DataAccess.Storage;

// Параметры подключения к S3-совместимому хранилищу (в т.ч. MinIO)
public sealed class S3StorageOptions
{
    public string ServiceUrl { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string Bucket { get; init; } = string.Empty;
}
