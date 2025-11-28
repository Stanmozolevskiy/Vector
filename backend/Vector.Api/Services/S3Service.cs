using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace Vector.Api.Services;

public class S3Service : IS3Service
{
    private readonly IConfiguration _configuration;
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3Service(IConfiguration configuration, IAmazonS3 s3Client)
    {
        _configuration = configuration;
        _s3Client = s3Client;
        _bucketName = _configuration["AWS:S3Bucket"] ?? throw new InvalidOperationException("S3 Bucket name is not configured");
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string key, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            CannedACL = S3CannedACL.PublicRead
        };

        await _s3Client.PutObjectAsync(request);
        
        var region = _configuration["AWS:Region"] ?? "us-east-1";
        return $"https://{_bucketName}.s3.{region}.amazonaws.com/{key}";
    }

    public async Task<bool> DeleteFileAsync(string key)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<string> GetFileUrlAsync(string key)
    {
        var region = _configuration["AWS:Region"] ?? "us-east-1";
        var url = $"https://{_bucketName}.s3.{region}.amazonaws.com/{key}";
        return Task.FromResult(url);
    }
}

