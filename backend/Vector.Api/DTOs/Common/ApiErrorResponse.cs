namespace Vector.Api.DTOs.Common;

/// <summary>
/// Standard API error response format
/// </summary>
public class ApiErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public string? Details { get; set; }
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    public ApiErrorResponse(string error, string? errorCode = null, string? details = null)
    {
        Error = error;
        ErrorCode = errorCode;
        Details = details;
    }

    public static ApiErrorResponse ValidationError(Dictionary<string, string[]> validationErrors)
    {
        return new ApiErrorResponse(
            "Validation failed",
            "VALIDATION_ERROR",
            "One or more validation errors occurred"
        )
        {
            ValidationErrors = validationErrors
        };
    }
}

