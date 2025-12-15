using System.ComponentModel.DataAnnotations;

namespace Vector.Api.DTOs.Question;

public class QuestionTestCaseDto
{
    public Guid Id { get; set; }
    public int TestCaseNumber { get; set; }
    public string Input { get; set; } = string.Empty;
    public string ExpectedOutput { get; set; } = string.Empty;
    public bool IsHidden { get; set; }
    public string? Explanation { get; set; }
}

public class CreateTestCaseDto
{
    [Required]
    public int TestCaseNumber { get; set; }
    
    /// <summary>
    /// Input data as plain string (will be used as stdin in Judge0)
    /// Example: "2\n7\n11\n15\n9" or "[1, 2, 3]\n7"
    /// </summary>
    [Required]
    public string Input { get; set; } = string.Empty;
    
    /// <summary>
    /// Expected output as plain string (will be used as expected_output in Judge0)
    /// Example: "[0, 1]" or "true"
    /// </summary>
    [Required]
    public string ExpectedOutput { get; set; } = string.Empty;
    
    public bool IsHidden { get; set; } = false;
    
    public string? Explanation { get; set; }
}

