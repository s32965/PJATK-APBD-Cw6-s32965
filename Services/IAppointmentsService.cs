using WebApplication1.DTOs;

namespace WebApplication1.Services;

public interface IAppointmentsService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? patientLastName, string? status,
        CancellationToken cancellationToken);

    Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken);

    Task<AppointmentDetailsDto> CreateAsync(CreateAppointmentRequestDto appointmentRequest,
        CancellationToken cancellationToken);
}