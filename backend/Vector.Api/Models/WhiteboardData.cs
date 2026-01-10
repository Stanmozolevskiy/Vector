using System.ComponentModel.DataAnnotations;

namespace Vector.Api.Models;

/// <summary>
/// Represents whiteboard data for a user
/// </summary>
public class WhiteboardData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Foreign key to User
    /// </summary>
    [Required]
    public Guid UserId { get; set; }
    
    /// <summary>
    /// Navigation property to User
    /// </summary>
    public User? User { get; set; }
    
    /// <summary>
    /// Optional question ID if whiteboard is associated with a question
    /// </summary>
    public Guid? QuestionId { get; set; }
    
    /// <summary>
    /// Navigation property to Question
    /// </summary>
    public InterviewQuestion? Question { get; set; }
    
    /// <summary>
    /// Excalidraw elements (JSON array)
    /// </summary>
    public string Elements { get; set; } = "[]";
    
    /// <summary>
    /// Excalidraw app state (JSON object)
    /// </summary>
    public string AppState { get; set; } = "{}";
    
    /// <summary>
    /// Excalidraw files (JSON object)
    /// </summary>
    public string Files { get; set; } = "{}";
    
    /// <summary>
    /// Timestamp when whiteboard was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Timestamp when whiteboard was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
