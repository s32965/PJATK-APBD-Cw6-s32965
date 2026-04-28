using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTOs;

public class ErrorReponseDto
{
    [Required(ErrorMessage = "Status code is required")]
    public required int StatusCode { get; init; }
    
    [Required(ErrorMessage = "Error message is required")]
    public required string Message { get; init; }
    
    public string? Details { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}