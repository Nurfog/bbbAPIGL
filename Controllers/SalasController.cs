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

    [HttpPost("salas")]
    [ProducesResponseType(typeof(CrearSalaResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CrearSala([FromBody] CrearSalaRequest request)
    {
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

    [HttpDelete("eliminar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> EliminarSala([FromBody] EliminarSalaRequest request)
    {
        try
        {
            var exito = await _salaService.EliminarSalaAsync(request.RoomId);
            if (exito) return NoContent();
            else return NotFound(new { error = $"No se encontró la sala con el ID: {request.RoomId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar la sala con ID: {RoomId}", request.RoomId);
            return StatusCode(500, new { error = "Ocurrió un error interno en el servidor." });
        }
    }

    [HttpPost("invitaciones")]
    [ProducesResponseType(typeof(EnviarInvitacionCursoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnviarInvitaciones([FromBody] EnviarInvitacionCursoRequest request)
    {
        try
        {
            var response = await _salaService.EnviarInvitacionesCursoAsync(request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
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

        return Ok(response); // Devuelve la lista (puede estar vacía)
    }
}