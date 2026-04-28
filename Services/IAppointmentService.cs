using WebApplication1.DTOs;

namespace WebApplication1;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAsync(CancellationToken cancellationToken);
}