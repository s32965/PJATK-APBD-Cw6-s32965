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

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateAppointmentRequestDto appointmentRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await appointmentsService.CreateAsync(appointmentRequest, cancellationToken);
            return Created($"/api/appointments/{result.IdAppointment}", result);
        } 
        catch (DateInvalidException e)
        {
            return BadRequest(e.Message);
        }
        catch (EmptyReasonException e)
        {
            return BadRequest(e.Message);
        }
        catch (ReasonTooLongException e)
        {
            return BadRequest(e.Message);
        }
        catch (PatientNotFoundException e)
        {
            return BadRequest(e.Message);
        }
        catch (PatientNotActiveException e)
        {
            return BadRequest(e.Message);
        }
        catch (DoctorNotFoundException e)
        {
            return BadRequest(e.Message);
        }
        catch (DoctorNotActiveException e)
        {
            return BadRequest(e.Message);
        }
        catch (DateConflictException e)
        {
            return Conflict(e.Message);
        }
        catch (Exception)
        {
            return Problem("Internal server error");
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] int id,
        [FromBody] UpdateRequestDto updateRequestDto,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await appointmentsService.UpdateAsync(id, updateRequestDto, cancellationToken);
            return Ok();
        }
        catch (EmptyReasonException e)
        {
            return BadRequest(e.Message);
        }
        catch (ReasonTooLongException e)
        {
            return BadRequest(e.Message);
        }
        catch (AppointmentNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (AppointmentAlreadyCompletedException e)
        {
            return Conflict(e.Message);
        }
        catch (PatientNotFoundException e)
        {
            return BadRequest(e.Message);
        }
        catch (PatientNotActiveException e)
        {
            return BadRequest(e.Message);
        }
        catch (DoctorNotFoundException e)
        {
            return BadRequest(e.Message);
        }
        catch (DoctorNotActiveException e)
        {
            return BadRequest(e.Message);
        }
        catch (DateConflictException e)
        {
            return Conflict(e.Message);
        }
        catch (Exception)
        {
            return Problem("Internal server error");
        }
    }
}