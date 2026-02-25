using Microsoft.AspNetCore.Mvc;
using bbbAPIGL.DTOs;
using bbbAPIGL.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace bbbAPIGL.Controllers;

[ApiController]
[Route("apiv2/emp")]
public class SalasEmpController : ControllerBase
{
    private readonly ISalaEmpresaService _salaService;
    private readonly ILogger<SalasEmpController> _logger;

    public SalasEmpController(ISalaEmpresaService salaService, ILogger<SalasEmpController> logger)
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
        catch (ApplicationException appEx)
        {
            _logger.LogWarning(appEx, "Error al crear la sala EMP: {Message}", appEx.Message);
            return BadRequest(new { error = appEx.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al crear la sala EMP.");
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
            return NotFound(new { error = $"No se encontró la sala EMP con el ID: {roomId}" });
       }
       catch (Exception ex)
       {
            _logger.LogError(ex, "Error inesperado al eliminar la sala EMP con ID: {RoomId}", roomId);
           return StatusCode(500, new { error = "Ocurrió un error interno en el servidor." });
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
            return NotFound(new { error = $"No se encontró un curso EMP con ID: {idCursoAbierto}" });
        }

        return Ok(response);
    }

    [HttpDelete("cursos/{idCursoAbierto:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarCurso(int idCursoAbierto)
    {
        try
        {
            var exito = await _salaService.EliminarCursoAsync(idCursoAbierto);
            if (exito) return NoContent();
            return NotFound(new { error = $"No se encontró el curso EMP con el ID: {idCursoAbierto}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar el curso EMP con ID: {IdCursoAbierto}", idCursoAbierto);
            return StatusCode(500, new { error = "Ocurrió un error interno en el servidor." });
        }
    }

    [HttpPost("reprogramar-sesion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReprogramarSesion([FromBody] ReprogramarSesionRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var exito = await _salaService.ReprogramarSesionAsync(request);
            if (exito) return Ok();
            return BadRequest(new { error = "No se pudo reprogramar la sesión EMP." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al reprogramar la sesión EMP.");
            return StatusCode(500, new { error = "Ocurrió un error interno en el servidor al reprogramar la sesión EMP." });
        }
    }

    [HttpGet("salas/{idCursoAbierto:int}/status")]
    [ProducesResponseType(typeof(SalaStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerEstadoSala(int idCursoAbierto)
    {
        try
        {
            var status = await _salaService.ObtenerEstadoSalaAsync(idCursoAbierto);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al obtener el estado de la sala para el curso EMP {IdCursoAbierto}.", idCursoAbierto);
            return StatusCode(500, new { error = "Ocurrió un error interno en el servidor al obtener el estado de la sala EMP." });
        }
    }
}
