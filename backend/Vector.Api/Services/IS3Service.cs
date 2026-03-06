namespace Vector.Api.Services;

/// <summary>
/// Service for handling S3 file operations
/// </summary>
public interface IS3Service
{
    /// <summary>
    /// Uploads a file to S3 and returns the public URL
    /// </summary>
    /// <param name="fileStream">The file stream to upload</param>
    /// <param name="fileName">The name of the file</param>
    /// <param name="contentType">The MIME type of the file</param>
    /// <param name="folder">Optional folder path within the bucket (e.g., "profile-pictures")</param>
    /// <returns>The public URL of the uploaded file</returns>
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string folder = "");

    /// <summary>
    /// Deletes a file from S3
    /// </summary>
    /// <param name="fileUrl">The URL of the file to delete</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteFileAsync(string fileUrl);

    /// <summary>
    /// Generates a presigned URL for temporary file access
    /// </summary>
    /// <param name="fileKey">The S3 key of the file</param>
    /// <param name="expirationMinutes">URL expiration time in minutes</param>
    /// <returns>The presigned URL</returns>
    Task<string> GetPresignedUrlAsync(string fileKey, int expirationMinutes = 60);
}
