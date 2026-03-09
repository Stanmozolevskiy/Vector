using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace Vector.Api.Services;

/// <summary>
/// File storage service backed by any S3-compatible provider (AWS S3 or Cloudflare R2).
/// Configure Storage:ServiceUrl + Storage:PublicUrl to use Cloudflare R2.
/// Leave those empty to fall back to standard AWS S3.
/// </summary>
public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3Service> _logger;
    private readonly string _bucketName;
    private readonly string _region;
    private readonly string? _publicBaseUrl;
    private readonly bool _useCustomProvider;

    public S3Service(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<S3Service> logger)
    {
        _s3Client = s3Client;
        _logger = logger;

        _bucketName = configuration["Storage:BucketName"]
            ?? configuration["AWS:S3:BucketName"]
            ?? throw new InvalidOperationException("Storage bucket name is not configured. Set Storage:BucketName or AWS:S3:BucketName.");

        _region = configuration["AWS:Region"] ?? "us-east-1";
        _publicBaseUrl = configuration["Storage:PublicUrl"];
        _useCustomProvider = !string.IsNullOrEmpty(configuration["Storage:ServiceUrl"]);
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "")
    {
        try
        {
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var key = string.IsNullOrEmpty(folder) ? uniqueFileName : $"{folder}/{uniqueFileName}";

            _logger.LogInformation("Uploading file: {Key}", key);

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = key,
                BucketName = _bucketName,
                ContentType = contentType
            };

            // ACLs are not supported on Cloudflare R2 — use bucket-level public access instead.
            // For standard AWS S3, apply per-object ACLs as before.
            if (!_useCustomProvider)
            {
                uploadRequest.CannedACL = (folder == "profile-pictures" || folder == "coach-applications" || folder == "question-videos" || folder == "dashboard-videos")
                    ? S3CannedACL.PublicRead
                    : S3CannedACL.Private;

                if (folder == "profile-pictures")
                {
                    uploadRequest.TagSet =
                    [
                        new Tag { Key = "public", Value = "true" },
                        new Tag { Key = "content-type", Value = "profile-picture" }
                    ];
                }
            }

            var transferUtility = new TransferUtility(_s3Client);
            await transferUtility.UploadAsync(uploadRequest);

            var url = BuildPublicUrl(key);
            _logger.LogInformation("File uploaded: {Url}", url);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
            throw new InvalidOperationException("Failed to upload file", ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            var uri = new Uri(fileUrl);
            var key = uri.AbsolutePath.TrimStart('/');

            _logger.LogInformation("Deleting file: {Key}", key);

            await _s3Client.DeleteObjectAsync(new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            });

            _logger.LogInformation("File deleted: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string fileKey, int expirationMinutes = 60)
    {
        try
        {
            var url = await _s3Client.GetPreSignedURLAsync(new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
            });
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for: {FileKey}", fileKey);
            throw new InvalidOperationException("Failed to generate presigned URL", ex);
        }
    }

    private string BuildPublicUrl(string key)
    {
        if (!string.IsNullOrEmpty(_publicBaseUrl))
            return $"{_publicBaseUrl.TrimEnd('/')}/{key}";

        return $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}";
    }
}
