namespace Vector.Api.DTOs.CodeExecution;

public class SupportedLanguageDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty; // Used in API requests
    public int Judge0LanguageId { get; set; }
    public string Version { get; set; } = string.Empty;
}

