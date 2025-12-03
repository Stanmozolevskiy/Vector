using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

namespace Vector.Api.Services;

/// <summary>
/// Service for handling S3 file operations
/// </summary>
public class S3Service : IS3Service
{
    private readonly IAmazonS3 _s3Client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<S3Service> _logger;
    private readonly string _bucketName;
    private readonly string _region;

    public S3Service(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<S3Service> logger)
    {
        _s3Client = s3Client;
        _configuration = configuration;
        _logger = logger;
        _bucketName = _configuration["AWS:S3:BucketName"] ?? throw new InvalidOperationException("S3 BucketName not configured");
        _region = _configuration["AWS:Region"] ?? "us-east-1";
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "")
    {
        try
        {
            // Generate unique file name
            var fileExtension = Path.GetExtension(fileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            var key = string.IsNullOrEmpty(folder) ? uniqueFileName : $"{folder}/{uniqueFileName}";

            _logger.LogInformation("Uploading file to S3: {Key}", key);

            var transferUtility = new TransferUtility(_s3Client);
            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = fileStream,
                Key = key,
                BucketName = _bucketName,
                ContentType = contentType,
                CannedACL = S3CannedACL.Private, // Private by default, use presigned URLs
            };

            // Add tags for profile pictures to allow public read
            if (folder == "profile-pictures")
            {
                uploadRequest.TagSet = new List<Tag>
                {
                    new Tag { Key = "public", Value = "true" }
                };
            }

            await transferUtility.UploadAsync(uploadRequest);

            // Return the URL
            var url = $"https://{_bucketName}.s3.{_region}.amazonaws.com/{key}";
            
            _logger.LogInformation("File uploaded successfully: {Url}", url);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to S3: {FileName}", fileName);
            throw new InvalidOperationException("Failed to upload file to S3", ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            // Extract key from URL
            var uri = new Uri(fileUrl);
            var key = uri.AbsolutePath.TrimStart('/');

            _logger.LogInformation("Deleting file from S3: {Key}", key);

            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);

            _logger.LogInformation("File deleted successfully: {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from S3: {FileUrl}", fileUrl);
            return false;
        }
    }

    public async Task<string> GetPresignedUrlAsync(string fileKey, int expirationMinutes = 60)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileKey,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };

            var url = await _s3Client.GetPreSignedURLAsync(request);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate presigned URL for: {FileKey}", fileKey);
            throw new InvalidOperationException("Failed to generate presigned URL", ex);
        }
    }
}
