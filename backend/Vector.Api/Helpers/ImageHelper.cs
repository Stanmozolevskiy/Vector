namespace Vector.Api.Helpers;

/// <summary>
/// Helper class for image validation
/// </summary>
public static class ImageHelper
{
    private const long MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
    private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

    /// <summary>
    /// Validates image file with comprehensive checks
    /// </summary>
    public static (bool IsValid, string? ErrorMessage, string? ErrorCode) ValidateImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return (false, "No file uploaded. Please select an image file.", "NO_FILE");
        }

        // Validate file size
        if (file.Length > MAX_FILE_SIZE)
        {
            var maxSizeMB = MAX_FILE_SIZE / (1024 * 1024);
            return (false, $"File size exceeds the maximum allowed size of {maxSizeMB}MB. Please choose a smaller image.", "FILE_TOO_LARGE");
        }

        // Validate MIME type
        var contentType = file.ContentType.ToLower();
        if (string.IsNullOrEmpty(contentType) || !AllowedMimeTypes.Contains(contentType))
        {
            return (false, "Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed.", "INVALID_FILE_TYPE");
        }

        // Validate file extension
        var extension = Path.GetExtension(file.FileName)?.ToLower();
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            return (false, "Invalid file extension. Only .jpg, .jpeg, .png, .gif, and .webp files are allowed.", "INVALID_EXTENSION");
        }

        // Additional validation: Check if extension matches content type
        var expectedExtension = contentType switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            _ => null
        };

        if (expectedExtension != null && !extension.Equals(expectedExtension, StringComparison.OrdinalIgnoreCase) && 
            !(extension == ".jpg" && expectedExtension == ".jpg"))
        {
            return (false, "File extension does not match the file type. Please ensure the file is a valid image.", "MISMATCHED_TYPE");
        }

        return (true, null, null);
    }

    /// <summary>
    /// Gets human-readable file size
    /// </summary>
    public static string GetFileSizeString(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

