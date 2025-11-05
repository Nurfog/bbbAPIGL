using bbbAPIGL.DTOs;
using bbbAPIGL.Models;
using bbbAPIGL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bbbAPIGL.Services;

/// <summary>
/// Servicio para la gestión de salas, incluyendo creación, eliminación y envío de invitaciones.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 23-10-2025
/// </summary>
public class SalaService : ISalaService
{
    private readonly ISalaRepository _salaRepository;
    private readonly ICursoRepository _cursoRepository;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ICalendarService _calendarService;
    private readonly ILogger<SalaService> _logger;

    public SalaService(
        ISalaRepository salaRepository,
        ICursoRepository cursoRepository,
        IConfiguration configuration,
        IEmailService emailService,
        ICalendarService calendarService,
        ILogger<SalaService> logger)
    {
        _salaRepository = salaRepository;
        _cursoRepository = cursoRepository;
        _configuration = configuration;
        _emailService = emailService;
        _calendarService = calendarService;
        _logger = logger;
    }

    /// <summary>
    /// Crea una nueva sala de reuniones virtual.
    /// </summary>
    /// <param name="request">Datos para la creación de la sala.</param>
    /// <returns>Una respuesta con los detalles de la sala creada.</returns>
    /// <exception cref="ApplicationException">Se lanza si ocurre un error al guardar la sala.</exception>
    public async Task<CrearSalaResponse> CrearNuevaSalaAsync(CrearSalaRequest request)
    {
        var meetingId = GeneraMeetingId();
        var friendlyId = GeneraFriendlyId();
        var claveModerador = GeneraRandomPassword(8);
        var claveEspectador = GeneraRandomPassword(8);
        var recordId = $"{meetingId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var publicUrl = _configuration["SalaSettings:PublicUrl"];

        var nuevaSala = new Sala
        {
            Nombre = request.Nombre,
            MeetingId = meetingId,
            FriendlyId = friendlyId,
            ClaveModerador = claveModerador,
            ClaveEspectador = claveEspectador
        };

        Guid? newRoomId = await _salaRepository.GuardarSalaAsync(nuevaSala, request.EmailCreador);

        if (newRoomId is null)
        {
            throw new ApplicationException("Hubo un problema al guardar la sala en la base de datos.");
        }

        var apiResponse = new CrearSalaResponse
        {
            RoomId = newRoomId.Value,
            NombreSala = nuevaSala.Nombre,
            UrlSala = $"{publicUrl}/rooms/{nuevaSala.FriendlyId}/join",
            ClaveModerador = nuevaSala.ClaveModerador,
            ClaveEspectador = nuevaSala.ClaveEspectador,
            MeetingId = nuevaSala.MeetingId,
            RecordId = recordId,
            FriendlyId = nuevaSala.FriendlyId
        };

        return apiResponse;
    }

    /// <summary>
    /// Elimina una sala y su evento de calendario asociado.
    /// </summary>
    /// <param name="roomId">El ID de la sala a eliminar.</param>
    /// <returns>Verdadero si la eliminación fue exitosa, falso en caso contrario.</returns>
    public async Task<bool> EliminarSalaAsync(Guid roomId)
    {
        var idCalendario = await _salaRepository.ObtenerIdCalendarioPorSalaIdAsync(roomId);

        if (!string.IsNullOrEmpty(idCalendario))
        {
            try
            {
                await _calendarService.EliminarEventoCalendarioAsync(idCalendario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar el evento de calendario con ID {IdCalendario} para la sala {RoomId}. La eliminación de la sala continuará.", idCalendario, roomId);
            }
        }

        var exitoPostgres = await _salaRepository.EliminarSalaAsync(roomId);

        if (exitoPostgres)
        {
            await _cursoRepository.DesasociarSalaDeCursosAsync(roomId);
        }

        return exitoPostgres;
    }

    /// <summary>
    /// Envía invitaciones de calendario a todos los alumnos de un curso.
    /// </summary>
    /// <param name="request">La solicitud con el ID del curso.</param>
    /// <returns>Una respuesta indicando el resultado del envío.</returns>
    /// <exception cref="InvalidOperationException">Se lanza si el curso no se encuentra o no tiene un horario definido.</exception>
    public async Task<EnviarInvitacionCursoResponse> EnviarInvitacionesCursoAsync(EnviarInvitacionCursoRequest request)
    {
        var sala = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(request.IdCursoAbierto);
        if (sala == null)
        {
            throw new InvalidOperationException("El curso abierto especificado no fue encontrado.");
        }

        var alumnosConCorreos = await _cursoRepository.ObtenerAlumnosConCorreosPorCursoAsync(request.IdCursoAbierto);
        if (alumnosConCorreos == null || !alumnosConCorreos.Any())
        {
            return new EnviarInvitacionCursoResponse { Mensaje = "No se encontraron alumnos para el curso.", CorreosEnviados = 0 };
        }

        var detallesSalaResponse = new CrearSalaResponse
        {
            RoomId = Guid.TryParse(sala.RoomId, out var roomId) ? roomId : Guid.Empty, 
            NombreSala = sala.NombreSala,
            FriendlyId = sala.FriendlyId ?? string.Empty,
            UrlSala = sala.UrlSala ?? string.Empty,
            ClaveEspectador = sala.ClaveEspectador ?? string.Empty,
            ClaveModerador = string.Empty,
            MeetingId = sala.MeetingId ?? string.Empty,
            RecordId = string.Empty
        };

        if (sala.FechaInicio == default || sala.FechaTermino == default || string.IsNullOrEmpty(sala.Dias))
        {
            throw new InvalidOperationException("El curso no tiene un horario definido (fechas o días de la semana) para crear un evento recurrente.");
        }

        int correosEnviados = 0;
        string? idCalendarioPrincipal = sala.IdCalendario; // Usar el ID de calendario existente si lo hay

        foreach (var alumno in alumnosConCorreos)
        {
            var invitacionExistente = await _cursoRepository.ObtenerInvitacionPorCursoAlumnoAsync(request.IdCursoAbierto, alumno.IdAlumno);

            if (invitacionExistente != null)
            {
                // Si ya existe una invitación, enviar un correo simple con la URL existente
                _logger.LogInformation("Invitación existente para el alumno {idAlumno}, enviando correo simple.", alumno.IdAlumno);
                var asunto = $"Recordatorio: Tu clase de {sala.NombreSala}";
                var replacements = new Dictionary<string, string>
                {
                    { "[**VAR_OC**]", sala.UrlSala ?? string.Empty },
                    { "[**VAR_F**]", sala.FechaInicio.ToString("dd-MM-yyyy") },
                    { "[**VAR_T**]", sala.NombreSala ?? string.Empty },
                    { "[**VAR_TD**]", sala.ClaveEspectador ?? string.Empty },
                };
                await _emailService.EnviarCorreoConPlantillaAsync(alumno.Email, asunto, replacements);

                correosEnviados++;
            }
            else
            {
                // Si no existe, enviar invitación de calendario y guardar
                try
                {
                    _logger.LogInformation("Enviando nueva invitación de calendario para el alumno {idAlumno}.", alumno.IdAlumno);
                    var idEventoCalendario = await _calendarService.EnviarInvitacionCalendarioAsync(
                        detallesSalaResponse, 
                        new List<string> { alumno.Email }, 
                        sala.FechaInicio, 
                        sala.FechaTermino, 
                        sala.Dias, 
                        sala.HoraInicio, 
                        sala.HoraTermino);

                    if (!string.IsNullOrEmpty(idEventoCalendario))
                    {
                        var nuevaInvitacion = new CursoAbiertoInvitacion
                        {
                            IdCursoAbiertoBbb = request.IdCursoAbierto,
                            IdAlumno = alumno.IdAlumno,
                            Url = detallesSalaResponse.UrlSala,
                            FechaCreacion = DateTime.UtcNow
                        };
                        await _cursoRepository.GuardarInvitacionAsync(nuevaInvitacion);
                        correosEnviados++;

                        // Si es la primera invitación de calendario para el curso, guardar su ID
                        if (string.IsNullOrEmpty(idCalendarioPrincipal))
                        {
                            idCalendarioPrincipal = idEventoCalendario;
                            await _cursoRepository.ActualizarIdCalendarioCursoAsync(request.IdCursoAbierto, idCalendarioPrincipal);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al enviar invitación de calendario para el alumno {idAlumno}.", alumno.IdAlumno);
                    // Continuar con los demás alumnos aunque falle uno
                }
            }
        }

        return new EnviarInvitacionCursoResponse
        {
            Mensaje = $"Proceso de envío de invitaciones completado. {correosEnviados} correos/invitaciones enviados.",
            CorreosEnviados = correosEnviados
        };
    }

    private static string GeneraMeetingId()
    {
        return Guid.NewGuid().ToString();
    }

    private static string GeneraFriendlyId()
    {
        return string.Join("-", Enumerable.Range(0, 4).Select(_ => GeneraRandomPassword(3)));
    }

    private static string GeneraRandomPassword(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
    
    /// <summary>
    /// Obtiene las URLs de las grabaciones de un curso.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una lista de DTOs con la información de las grabaciones, o null si el curso no existe.</returns>
    public async Task<List<GrabacionDto>?> ObtenerUrlsGrabacionesAsync(int idCursoAbierto)
    {
        var curso = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(idCursoAbierto);
        if (curso == null || !Guid.TryParse(curso.RoomId, out var roomId))
        {
            return null; 
        }

        var recordingInfos = await _salaRepository.ObtenerTodosLosRecordIdsPorRoomIdAsync(roomId);

        if (recordingInfos == null || !recordingInfos.Any())
        {
            return new List<GrabacionDto>();
        }

        var publicUrl = _configuration["SalaSettings:PublicUrl"];
        
        var grabacionesDto = recordingInfos.Select(rec => new GrabacionDto
        {
            RecordId = rec.RecordId,
            CreatedAt = rec.CreatedAt.ToString("yyyy-MM-dd"),
            PlaybackUrl = $"{publicUrl}/playback/presentation/2.3/{rec.RecordId}"
        }).ToList();

        return grabacionesDto;
    }

    /// <summary>
    /// Envía una invitación de calendario individual a un alumno para un curso.
    /// </summary>
    /// <param name="request">La solicitud con los IDs del curso y del alumno.</param>
    /// <returns>Una respuesta indicando el resultado del envío.</returns>
    /// <exception cref="InvalidOperationException">Se lanza si el curso no se encuentra, no tiene horario o si hay un error con el servicio de calendario.</exception>
    public async Task<EnviarInvitacionCursoResponse> EnviarInvitacionIndividualAsync(EnviarInvitacionIndividualRequest request)
    {
        var sala = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(request.IdCursoAbierto);
        if (sala == null)
        {
            throw new InvalidOperationException("El curso abierto especificado no fue encontrado.");
        }

        if (sala.FechaInicio == default || sala.FechaTermino == default || string.IsNullOrEmpty(sala.Dias) || sala.HoraInicio == default || sala.HoraTermino == default)
        {
            _logger.LogInformation("El curso {IdCursoAbierto} no tiene un horario definido. Se intentará actualizar desde la fuente externa.", request.IdCursoAbierto);
            var actualizado = await _cursoRepository.ActualizarHorarioDesdeFuenteExternaAsync(request.IdCursoAbierto);
            if (actualizado)
            {
                _logger.LogInformation("Horario del curso {IdCursoAbierto} actualizado exitosamente.", request.IdCursoAbierto);
                sala = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(request.IdCursoAbierto);
                if (sala == null)
                {
                    throw new InvalidOperationException("El curso abierto especificado no fue encontrado después de la actualización.");
                }
            }
            else
            {
                _logger.LogWarning("No se pudo actualizar el horario para el curso {IdCursoAbierto}.", request.IdCursoAbierto);
            }
        }

        var correoAlumno = await _cursoRepository.ObtenerCorreoPorAlumnoAsync(request.IdAlumno.ToString(), request.IdCursoAbierto);
        if (string.IsNullOrEmpty(correoAlumno))
        {
            return new EnviarInvitacionCursoResponse { Mensaje = "No se encontró un alumno con el ID especificado para este curso.", CorreosEnviados = 0 };
        }

        var detallesSalaResponse = new CrearSalaResponse
        {
            RoomId = Guid.TryParse(sala.RoomId, out var roomId) ? roomId : Guid.Empty,
            NombreSala = sala.NombreSala,
            FriendlyId = sala.FriendlyId ?? string.Empty,
            UrlSala = sala.UrlSala ?? string.Empty,
            ClaveEspectador = sala.ClaveEspectador ?? string.Empty,
            ClaveModerador = string.Empty,
            MeetingId = sala.MeetingId ?? string.Empty,
            RecordId = string.Empty
        };

        if (sala.FechaInicio == default || sala.FechaTermino == default || string.IsNullOrEmpty(sala.Dias))
        {
            throw new InvalidOperationException("El curso no tiene un horario definido (fechas o días de la semana) para crear un evento recurrente.");
        }

        var invitacionExistente = await _cursoRepository.ObtenerInvitacionPorCursoAlumnoAsync(request.IdCursoAbierto, request.IdAlumno);

        if (invitacionExistente != null)
        {
            _logger.LogInformation("Invitación existente para el alumno {idAlumno}, enviando correo simple.", request.IdAlumno);
            var asunto = $"Recordatorio: Tu clase de {sala.NombreSala}";
            var replacements = new Dictionary<string, string>
            {
                { "[**VAR_OC**]", sala.UrlSala ?? string.Empty },
                { "[**VAR_F**]", sala.FechaInicio.ToString("dd-MM-yyyy") },
                { "[**VAR_T**]", sala.NombreSala ?? string.Empty },
                { "[**VAR_TD**]", sala.ClaveEspectador ?? string.Empty },
            };
            await _emailService.EnviarCorreoConPlantillaAsync(correoAlumno, asunto, replacements);
            return new EnviarInvitacionCursoResponse
            {
                Mensaje = "Recordatorio enviado exitosamente.",
                CorreosEnviados = 1
            };
        }
        else
        {
            try
            {
                _logger.LogInformation("Enviando nueva invitación de calendario para el alumno {idAlumno}.", request.IdAlumno);
                
                string? idEventoCalendario = null;

                if (string.IsNullOrEmpty(sala.IdCalendario))
                {
                    // Si no hay IdCalendario, se crea un nuevo evento
                    idEventoCalendario = await _calendarService.EnviarInvitacionCalendarioAsync(
                        detallesSalaResponse,
                        new List<string> { correoAlumno },
                        sala.FechaInicio,
                        sala.FechaTermino,
                        sala.Dias,
                        sala.HoraInicio,
                        sala.HoraTermino);

                    if (!string.IsNullOrEmpty(idEventoCalendario))
                    {
                        await _cursoRepository.ActualizarIdCalendarioCursoAsync(request.IdCursoAbierto, idEventoCalendario);
                    }
                }
                else
                {
                    // Si ya existe un IdCalendario, se actualiza el evento existente
                    idEventoCalendario = sala.IdCalendario;
                    var todosLosCorreos = await _cursoRepository.ObtenerCorreosPorCursoAsync(request.IdCursoAbierto.ToString());
                    if (!todosLosCorreos.Contains(correoAlumno))
                    {
                        todosLosCorreos.Add(correoAlumno);
                    }

                    await _calendarService.ActualizarEventoCalendarioAsync(
                        idEventoCalendario,
                        detallesSalaResponse,
                        todosLosCorreos,
                        sala.FechaInicio,
                        sala.FechaTermino,
                        sala.Dias,
                        sala.HoraInicio,
                        sala.HoraTermino);
                }

                if (!string.IsNullOrEmpty(idEventoCalendario))
                {
                    var nuevaInvitacion = new CursoAbiertoInvitacion
                    {
                        IdCursoAbiertoBbb = request.IdCursoAbierto,
                        IdAlumno = request.IdAlumno,
                        Url = detallesSalaResponse.UrlSala,
                        FechaCreacion = DateTime.UtcNow
                    };
                    await _cursoRepository.GuardarInvitacionAsync(nuevaInvitacion);
                }

                return new EnviarInvitacionCursoResponse
                {
                    Mensaje = "Invitación enviada exitosamente.",
                    CorreosEnviados = 1
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar la invitación individual con el servicio de calendario.");
                throw new InvalidOperationException("Error al procesar la invitación con el servicio de calendario. Verifica la configuración y los permisos.", ex);
            }
        }
    }

    /// <summary>
    /// Actualiza un evento de calendario existente para un curso.
    /// </summary>
    /// <param name="request">La solicitud con los detalles de la actualización.</param>
    /// <returns>Una respuesta indicando el resultado de la actualización.</returns>
    /// <exception cref="InvalidOperationException">Se lanza si el curso no se encuentra, no tiene evento de calendario o si hay un error con el servicio de calendario.</exception>
    public async Task<EnviarInvitacionCursoResponse> ActualizarInvitacionesCursoAsync(ActualizarEventoCalendarioRequest request)
    {
        var sala = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(request.IdCursoAbierto);
        if (sala == null)
        {
            throw new InvalidOperationException("El curso abierto especificado no fue encontrado.");
        }

        if (string.IsNullOrEmpty(sala.IdCalendario))
        {
            throw new InvalidOperationException("La sala no tiene un evento de calendario asociado.");
        }

        var correos = request.CorreosParticipantes ?? await _cursoRepository.ObtenerCorreosPorCursoAsync(request.IdCursoAbierto.ToString());
        if (correos == null || !correos.Any())
        {
            return new EnviarInvitacionCursoResponse { Mensaje = "No se encontraron alumnos para el curso.", CorreosEnviados = 0 };
        }

        var detallesSalaResponse = new CrearSalaResponse
        {
            RoomId = Guid.TryParse(sala.RoomId, out var roomId) ? roomId : Guid.Empty,
            NombreSala = sala.NombreSala,
            FriendlyId = sala.FriendlyId ?? string.Empty,
            UrlSala = sala.UrlSala ?? string.Empty,
            ClaveEspectador = sala.ClaveEspectador ?? string.Empty,
            ClaveModerador = string.Empty,
            MeetingId = sala.MeetingId ?? string.Empty,
            RecordId = string.Empty
        };

        var fechaInicio = request.FechaInicio ?? sala.FechaInicio;
        var fechaTermino = request.FechaTermino ?? sala.FechaTermino;
        var dias = request.DiasSemana ?? sala.Dias;
        var horaInicio = request.HoraInicio ?? sala.HoraInicio;
        var horaTermino = request.HoraTermino ?? sala.HoraTermino;

        if (fechaInicio == default || fechaTermino == default || string.IsNullOrEmpty(dias))
        {
            throw new InvalidOperationException("El curso no tiene un horario definido (fechas o días de la semana) para crear un evento recurrente.");
        }

        try
        {
            await _calendarService.ActualizarEventoCalendarioAsync(sala.IdCalendario, detallesSalaResponse, correos, fechaInicio, fechaTermino, dias, horaInicio, horaTermino);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la actualización de la invitación con el servicio de calendario.");
            throw new InvalidOperationException("Error al procesar la actualización de la invitación con el servicio de calendario. Verifica la configuración y los permisos.", ex);
        }

        return new EnviarInvitacionCursoResponse
        {
            Mensaje = "Invitaciones actualizadas exitosamente.",
            CorreosEnviados = correos.Count
        };
    }

    public async Task<bool> EliminarCursoAsync(int idCursoAbierto)
    {
        _logger.LogInformation("Iniciando eliminación del curso {IdCursoAbierto}", idCursoAbierto);

        var invitaciones = await _cursoRepository.ObtenerInvitacionesPorCursoAsync(idCursoAbierto);

        foreach (var invitacion in invitaciones)
        {
            if (!string.IsNullOrEmpty(invitacion.IdCalendario))
            {
                try
                {
                    await _calendarService.EliminarEventoCalendarioAsync(invitacion.IdCalendario);
                    _logger.LogInformation("Evento de calendario {IdCalendario} eliminado para la invitación.", invitacion.IdCalendario);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al eliminar el evento de calendario {IdCalendario}. La eliminación del curso continuará.", invitacion.IdCalendario);
                }
            }
        }

        await _cursoRepository.EliminarInvitacionesPorCursoAsync(idCursoAbierto);
        _logger.LogInformation("Todas las invitaciones para el curso {IdCursoAbierto} han sido eliminadas.", idCursoAbierto);

        var exito = await _cursoRepository.EliminarCursoAsync(idCursoAbierto);
        if (exito)
        {
            _logger.LogInformation("Curso {IdCursoAbierto} eliminado exitosamente.", idCursoAbierto);
        }
        else
        {
            _logger.LogWarning("No se encontró o no se pudo eliminar el curso {IdCursoAbierto}.", idCursoAbierto);
        }

        return exito;
    }
}