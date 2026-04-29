using WebApplication1.DTOs;

namespace WebApplication1.Services;

public interface IAppointmentsService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? patientLastName, string? status,
        CancellationToken cancellationToken);
}