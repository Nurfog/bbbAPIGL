using Microsoft.AspNetCore.Mvc;
using bbbAPIGL.DTOs;
using bbbAPIGL.Services;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Controlador para la gestión de salas y sus invitaciones.
/// </summary>
namespace bbbAPIGL.Controllers;

[ApiController]
[Route("apiv2")]
public class SalasController : ControllerBase
{
    private readonly ISalaService _salaService;
    private readonly ILogger<SalasController> _logger;

    public SalasController(ISalaService salaService, ILogger<SalasController> logger)
    {
        _salaService = salaService;
        _logger = logger;
    }

    /// <summary>
    /// Crea una nueva sala.
    /// </summary>
    /// <param name="request">Datos para la creación de la sala.</param>
    /// <returns>Un objeto CrearSalaResponse con los detalles de la sala creada.</returns>
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

    /// <summary>
    /// Elimina una sala existente.
    /// </summary>
    /// <param name="roomId">El ID de la sala a eliminar.</param>
    /// <returns>NoContent si la sala se eliminó con éxito, NotFound si no se encontró la sala.</returns>
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

    /// <summary>
    /// Envía invitaciones para un curso abierto.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Un objeto EnviarInvitacionCursoResponse con el resultado de la operación.</returns>
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

    /// <summary>
    /// Envía una invitación individual para un curso abierto.
    /// </summary>
    /// <param name="idAlumno">El ID del alumno.</param>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Un objeto EnviarInvitacionCursoResponse con el resultado de la operación.</returns>
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

    /// <summary>
    /// Actualiza las invitaciones para un curso abierto.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <param name="requestBody">Datos para actualizar el evento del calendario.</param>
    /// <returns>Un objeto EnviarInvitacionCursoResponse con el resultado de la operación.</returns>
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

    /// <summary>
    /// Obtiene las URLs de grabación para un curso abierto.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una lista de objetos GrabacionDto con las URLs de grabación.</returns>
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

    /// <summary>
    /// Elimina un curso abierto y todas sus invitaciones asociadas.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto a eliminar.</param>
    /// <returns>NoContent si el curso se eliminó con éxito, NotFound si no se encontró el curso.</returns>
    [HttpDelete("cursos/{idCursoAbierto:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EliminarCurso(int idCursoAbierto)
    {
        try
        {
            var exito = await _salaService.EliminarCursoAsync(idCursoAbierto);
            if (exito) return NoContent();
            return NotFound(new { error = $"No se encontró el curso con el ID: {idCursoAbierto}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al eliminar el curso con ID: {IdCursoAbierto}", idCursoAbierto);
            return StatusCode(500, new { error = "Ocurrió un error interno en el servidor." });
        }
    }
}