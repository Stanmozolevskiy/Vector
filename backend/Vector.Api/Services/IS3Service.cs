namespace Vector.Api.Services;

public interface IS3Service
{
    Task<string> UploadFileAsync(Stream fileStream, string key, string contentType);
    Task<bool> DeleteFileAsync(string key);
    Task<string> GetFileUrlAsync(string key);
}

