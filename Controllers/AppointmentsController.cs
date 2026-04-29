using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTOs;
using WebApplication1.Services;
using WebApplication1.Exceptions;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentsService appointmentsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(
        [FromQuery] string? patientLastName,
        [FromQuery] string? status,
        CancellationToken cancellationToken
        )
    {
        var result = await appointmentsService.GetAllAsync(patientLastName, status, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken
        )
    {
        try
        {
            return Ok(await appointmentsService.GetByIdAsync(id, cancellationToken));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}