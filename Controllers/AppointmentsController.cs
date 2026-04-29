using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTOs;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentsService appointmentsService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AppointmentListDto>> GetAllAsync(
        [FromQuery] string? patientLastName,
        [FromQuery] string? status,
        CancellationToken cancellationToken
        )
    {
        var result = await appointmentsService.GetAllAsync(patientLastName, status, cancellationToken);
        return Ok(result);
    }
}