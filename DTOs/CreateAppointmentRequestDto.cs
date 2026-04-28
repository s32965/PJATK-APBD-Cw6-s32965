using System.ComponentModel.DataAnnotations;
namespace WebApplication1.DTOs;

public class CreateAppointmentRequestDto
{
    [Required(ErrorMessage = "Patient id is required.")]
    public int IdPatient { get; set; }
    
    [Required(ErrorMessage = "Doctor id is required.")]
    public int IdDoctor { get; set; }

    [Required(ErrorMessage = "Appointment date is required.")]
    public DateTime AppointmentDate { get; set; }

    [Required(ErrorMessage = "Appointment reason is required.")]
    [MaxLength(250, ErrorMessage = "Appointment reason must be less than 250 characters.")]
    public string Reason { get; set; } = string.Empty;
}