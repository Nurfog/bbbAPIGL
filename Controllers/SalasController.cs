using Microsoft.AspNetCore.Mvc;
using bbbAPIGL.DTOs;
using bbbAPIGL.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace bbbAPIGL.Controllers;

[ApiController]
public class SalasController : ControllerBase
{
    private readonly ISalaService _salaService;
    private readonly ILogger<SalasController> _logger;

    public SalasController(ISalaService salaService, ILogger<SalasController> logger)
    {
        _salaService = salaService;
        _logger = logger;
    }

    [HttpPost("salas/{nombre}/{emailCreador}")]
    [ProducesResponseType(typeof(CrearSalaResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearSala([FromRoute] string nombre, [FromRoute] string emailCreador, [FromQuery] string? correosParticipantes = null)
    {
        var request = new CrearSalaRequest
        {
            Nombre = nombre,
            EmailCreador = emailCreador
        };

        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var response = await _salaService.CrearNuevaSalaAsync(request);
            return CreatedAtAction(null, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear la sala.");
            return StatusCode(500, new { error = "Ocurrió un error interno en el servidor." });
        }
    }

    [HttpDelete("salas/{roomId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarSala(Guid roomId)
    {
       try
       {
           var exito = await _salaService.EliminarSalaAsync(roomId);
           if (exito) return NoContent();
            return NotFound(new { error = $"No se encontró la sala con el ID: {roomId}" });
       }
       catch (Exception ex)
       {
            _logger.LogError(ex, "Error inesperado al eliminar la sala con ID: {RoomId}", roomId);
           return StatusCode(500, new { error = "Ocurrió un error interno en el servidor." });
       }
    }

    [HttpPost("invitaciones/{idCursoAbierto:int}")]
    [ProducesResponseType(typeof(EnviarInvitacionCursoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnviarInvitaciones([FromRoute] int idCursoAbierto)
    {
        var request = new EnviarInvitacionCursoRequest { IdCursoAbierto = idCursoAbierto };
        try
        {
            var response = await _salaService.EnviarInvitacionesCursoAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar invitaciones.");
            return StatusCode(500, new { error = "Ocurrió un error interno en el servidor al enviar las invitaciones." });
        }
    }

    [HttpPost("invitaciones/individual/{idAlumno}/{idCursoAbierto:int}")]
    [ProducesResponseType(typeof(EnviarInvitacionCursoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnviarInvitacionIndividual([FromRoute] string idAlumno, [FromRoute] int idCursoAbierto)
    {
        var request = new EnviarInvitacionIndividualRequest { IdAlumno = idAlumno, IdCursoAbierto = idCursoAbierto };
        try
        {
            var response = await _salaService.EnviarInvitacionIndividualAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al enviar invitación individual.");
            return StatusCode(500, new { error = "Ocurrió un error interno en el servidor al enviar la invitación." });
        }
    }

    [HttpPut("invitaciones/{idCursoAbierto:int}")]
    [ProducesResponseType(typeof(EnviarInvitacionCursoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ActualizarInvitaciones([FromRoute] int idCursoAbierto, [FromBody] ActualizarEventoCalendarioRequest requestBody)
    {
        requestBody.IdCursoAbierto = idCursoAbierto;
        try
        {
            var response = await _salaService.ActualizarInvitacionesCursoAsync(requestBody);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al actualizar invitaciones.");
            return StatusCode(500, new { error = "Ocurrió un error interno en el servidor al actualizar las invitaciones." });
        }
    }

    [HttpGet("grabaciones/{idCursoAbierto}")]
    [ProducesResponseType(typeof(List<GrabacionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerUrlGrabacion(int idCursoAbierto)
    {
        var response = await _salaService.ObtenerUrlsGrabacionesAsync(idCursoAbierto);

        if (response == null)
        {
            return NotFound(new { error = $"No se encontró un curso con ID: {idCursoAbierto}" });
        }

        return Ok(response);
    }
}