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
using bbbAPIGL.Utils;

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
    private readonly IAcademicCalendarService _academicCalendarService;
    private readonly ILogger<SalaService> _logger;

    public SalaService(
        ISalaRepository salaRepository,
        ICursoRepository cursoRepository,
        IConfiguration configuration,
        IEmailService emailService,
        ICalendarService calendarService,
        IAcademicCalendarService academicCalendarService,
        ILogger<SalaService> logger)
    {
        _salaRepository = salaRepository;
        _cursoRepository = cursoRepository;
        _configuration = configuration;
        _emailService = emailService;
        _calendarService = calendarService;
        _academicCalendarService = academicCalendarService;
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
        var idCalendario = await _cursoRepository.ObtenerIdCalendarioPorRoomIdAsync(roomId);

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

        if (sala.FechaInicio == default || sala.FechaTermino == default || string.IsNullOrEmpty(sala.Dias))
        {
            throw new InvalidOperationException("El curso no tiene un horario definido (fechas o días de la semana) para crear un evento recurrente.");
        }

        var detallesSalaResponse = new CrearSalaResponse
        {
            RoomId = Guid.TryParse(sala.RoomId, out var roomId) ? roomId : Guid.Empty,
            NombreSala = sala.NombreSala ?? string.Empty,
            FriendlyId = sala.FriendlyId ?? string.Empty,
            UrlSala = sala.UrlSala ?? string.Empty,
            ClaveEspectador = sala.ClaveEspectador ?? string.Empty,
            ClaveModerador = string.Empty,
            MeetingId = sala.MeetingId ?? string.Empty,
            RecordId = string.Empty
        };

        var correosAlumnos = alumnosConCorreos.Select(a => a.Email).ToList();
        string? idCalendario = sala.IdCalendario;

        try
        {
            if (string.IsNullOrEmpty(idCalendario))
            {
                _logger.LogInformation("No existe evento de calendario para el curso {IdCursoAbierto}. Creando uno nuevo.", request.IdCursoAbierto);
                idCalendario = await _calendarService.EnviarInvitacionCalendarioAsync(
                    detallesSalaResponse,
                    correosAlumnos,
                    sala.FechaInicio,
                    sala.FechaTermino,
                    sala.Dias!,
                    sala.HoraInicio,
                    sala.HoraTermino);

                if (!string.IsNullOrEmpty(idCalendario))
                {
                    await _cursoRepository.ActualizarIdCalendarioCursoAsync(request.IdCursoAbierto, idCalendario);
                    _logger.LogInformation("Nuevo evento de calendario {idCalendario} creado y asociado al curso {IdCursoAbierto}.", idCalendario, request.IdCursoAbierto);
                }
            }
            else
            {
                _logger.LogInformation("Evento de calendario {idCalendario} existente para el curso {IdCursoAbierto}. Actualizando participantes.", idCalendario, request.IdCursoAbierto);
                await _calendarService.ActualizarEventoCalendarioAsync(
                    idCalendario,
                    detallesSalaResponse,
                    correosAlumnos,
                    sala.FechaInicio,
                    sala.FechaTermino,
                    sala.Dias!,
                    sala.HoraInicio,
                    sala.HoraTermino);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear o actualizar el evento de calendario para el curso {IdCursoAbierto}.", request.IdCursoAbierto);
            throw new InvalidOperationException("Ocurrió un error al procesar la invitación de calendario.", ex);
        }

        if (string.IsNullOrEmpty(idCalendario))
        {
            throw new InvalidOperationException("No se pudo crear o actualizar el evento en el calendario.");
        }

        // Guardar las invitaciones en la base de datos para seguimiento interno
        foreach (var alumno in alumnosConCorreos)
        {
            var invitacionExistente = await _cursoRepository.ObtenerInvitacionPorCursoAlumnoAsync(request.IdCursoAbierto, alumno.IdAlumno);
            if (invitacionExistente == null)
            {
                var nuevaInvitacion = new CursoAbiertoInvitacion
                {
                    IdCursoAbiertoBbb = request.IdCursoAbierto,
                    IdAlumno = alumno.IdAlumno,
                    Url = detallesSalaResponse.UrlSala,
                    FechaCreacion = DateTime.UtcNow
                };
                await _cursoRepository.GuardarInvitacionAsync(nuevaInvitacion);
            }
                    }
        
                    // Enviar correo recordatorio a los alumnos
                    var emailSubject = $"Recordatorio: Información de tu clase de {detallesSalaResponse.NombreSala}";

        
                    foreach (var alumno in alumnosConCorreos)
                    {
                        var replacements = new Dictionary<string, string>
                        {
                            { "[**VAR_OC**]", detallesSalaResponse.UrlSala },
                            { "[**VAR_F**]", sala.FechaInicio.ToString("dd-MM-yyyy HH:mm") },
                            { "[**VAR_T**]", detallesSalaResponse.NombreSala ?? string.Empty },
                            { "[**VAR_TD**]", detallesSalaResponse.ClaveEspectador ?? string.Empty }
                        };
        
                        await _emailService.EnviarCorreoConPlantillaAsync(alumno.Email, emailSubject, replacements);
                        _logger.LogInformation("Correo recordatorio enviado a {Email} para el curso {IdCursoAbierto}.", alumno.Email, request.IdCursoAbierto);
                    }
        
                    return new EnviarInvitacionCursoResponse
                    {
                        Mensaje = $"Proceso de envío de invitaciones y recordatorios completado. {correosAlumnos.Count} participantes gestionados en el evento de calendario y {alumnosConCorreos.Count} correos recordatorios enviados.",
                        CorreosEnviados = correosAlumnos.Count
                    };    }

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
                throw new InvalidOperationException("El curso no tiene un horario definido (fechas o días de la semana) para crear un evento recurrente.");
            }
        }

        var correoAlumno = await _cursoRepository.ObtenerCorreoPorAlumnoAsync(request.IdAlumno.ToString(), request.IdCursoAbierto);
        if (string.IsNullOrEmpty(correoAlumno))
        {
            return new EnviarInvitacionCursoResponse { Mensaje = "No se encontró un alumno con el ID especificado para este curso.", CorreosEnviados = 0 };
        }

        var invitacionExistente = await _cursoRepository.ObtenerInvitacionPorCursoAlumnoAsync(request.IdCursoAbierto, request.IdAlumno);
        if (invitacionExistente != null)
        {
            _logger.LogInformation("El alumno {idAlumno} ya tiene una invitación para el curso {idCursoAbierto}. Se procederá a verificar/actualizar el evento de calendario y enviar recordatorio.", request.IdAlumno, request.IdCursoAbierto);
        }

        var detallesSalaResponse = new CrearSalaResponse
        {
            RoomId = Guid.TryParse(sala.RoomId, out var roomId) ? roomId : Guid.Empty,
            NombreSala = sala.NombreSala ?? string.Empty,
            FriendlyId = sala.FriendlyId ?? string.Empty,
            UrlSala = sala.UrlSala ?? string.Empty,
            ClaveEspectador = sala.ClaveEspectador ?? string.Empty,
            ClaveModerador = string.Empty,
            MeetingId = sala.MeetingId ?? string.Empty,
            RecordId = string.Empty
        };

        try
        {
            string? idCalendario = sala.IdCalendario;
            if (string.IsNullOrEmpty(idCalendario))
            {
                _logger.LogInformation("Creando nuevo evento de calendario para el curso {IdCursoAbierto} con el primer alumno {idAlumno}.", request.IdCursoAbierto, request.IdAlumno);
                idCalendario = await _calendarService.EnviarInvitacionCalendarioAsync(
                    detallesSalaResponse,
                    new List<string> { correoAlumno },
                    sala.FechaInicio,
                    sala.FechaTermino,
                    sala.Dias!,
                    sala.HoraInicio,
                    sala.HoraTermino);

                if (!string.IsNullOrEmpty(idCalendario))
                {
                    await _cursoRepository.ActualizarIdCalendarioCursoAsync(request.IdCursoAbierto, idCalendario);
                }
            }
            else
            {
                _logger.LogInformation("Actualizando evento de calendario existente {idCalendario} para añadir al alumno {idAlumno}.", idCalendario, request.IdAlumno);
                var todosLosCorreos = await _cursoRepository.ObtenerCorreosPorCursoAsync(request.IdCursoAbierto.ToString());
                if (!todosLosCorreos.Contains(correoAlumno))
                {
                    todosLosCorreos.Add(correoAlumno);
                }

                await _calendarService.ActualizarEventoCalendarioAsync(
                    idCalendario,
                    detallesSalaResponse,
                    todosLosCorreos,
                    sala.FechaInicio,
                    sala.FechaTermino,
                    sala.Dias!,
                    sala.HoraInicio,
                    sala.HoraTermino);
            }

            if (!string.IsNullOrEmpty(idCalendario))
            {
                var nuevaInvitacion = new CursoAbiertoInvitacion
                {
                    IdCursoAbiertoBbb = request.IdCursoAbierto,
                    IdAlumno = request.IdAlumno,
                    Url = detallesSalaResponse.UrlSala,
                    FechaCreacion = DateTime.UtcNow
                };
                await _cursoRepository.GuardarInvitacionAsync(nuevaInvitacion);

                // Enviar correo recordatorio al alumno individual
                var emailSubject = $"Recordatorio: Información de tu clase de {detallesSalaResponse.NombreSala}";
                var replacements = new Dictionary<string, string>
                {
                    { "[**VAR_OC**]", detallesSalaResponse.UrlSala },
                    { "[**VAR_F**]", sala.FechaInicio.ToString("dd-MM-yyyy HH:mm") },
                    { "[**VAR_T**]", detallesSalaResponse.NombreSala ?? string.Empty },
                    { "[**VAR_TD**]", detallesSalaResponse.ClaveEspectador }
                };

                await _emailService.EnviarCorreoConPlantillaAsync(correoAlumno, emailSubject, replacements);
                _logger.LogInformation("Correo recordatorio enviado a {Email} para el curso {IdCursoAbierto}.", correoAlumno, request.IdCursoAbierto);
            }
            else
            {
                 throw new InvalidOperationException("No se pudo crear o actualizar el evento en el calendario.");
            }

            return new EnviarInvitacionCursoResponse
            {
                Mensaje = "Invitación enviada y/o actualizada exitosamente.",
                CorreosEnviados = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la invitación individual con el servicio de calendario.");
            throw new InvalidOperationException("Error al procesar la invitación con el servicio de calendario. Verifica la configuración y los permisos.", ex);
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
            NombreSala = sala.NombreSala ?? string.Empty,
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

    public async Task<bool> ReprogramarSesionAsync(ReprogramarSesionRequest request)
    {
        try
        {
            var sala = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(request.IdCursoAbierto);
            if (sala == null || string.IsNullOrEmpty(sala.IdCalendario))
            {
                _logger.LogError("No se encontró la sala o el IdCalendario para el curso {IdCursoAbierto}.", request.IdCursoAbierto);
                return false;
            }

            var fechaOriginal = request.FechaOriginalSesion;
            var idCursoAbierto = request.IdCursoAbierto;

            // 1. Obtener el evento recurrente maestro para tener los detalles
            var recurringEvent = await _calendarService.ObtenerEventoAsync(sala.IdCalendario);
            if (recurringEvent == null)
            {
                _logger.LogError("No se encontró el evento recurrente maestro con ID {IdCalendario}.", sala.IdCalendario);
                return false;
            }

            // 2. Encontrar y cancelar la instancia específica del evento recurrente
            var instances = await _calendarService.ObtenerInstanciasDeEventoAsync(sala.IdCalendario);
            
            var instanceToCancel = instances.FirstOrDefault(i =>
            {
                if (i.Start.DateTimeDateTimeOffset.HasValue)
                {
                    return i.Start.DateTimeDateTimeOffset.Value.Date == fechaOriginal.ToDateTime(TimeOnly.MinValue).Date;
                }
                if (!string.IsNullOrEmpty(i.Start.Date))
                {
                    return DateTime.Parse(i.Start.Date).Date == fechaOriginal.ToDateTime(TimeOnly.MinValue).Date;
                }
                return false;
            });

            if (instanceToCancel != null && instanceToCancel.Status != "cancelled")
            {
                await _calendarService.CancelarInstanciaAsync(instanceToCancel, sendNotifications: false);
                _logger.LogInformation("Instancia del evento con ID de instancia {instanceId} (del evento maestro {IdCalendario}) en la fecha {fechaOriginal} fue cancelada.", instanceToCancel.Id, sala.IdCalendario, fechaOriginal.ToString("yyyy-MM-dd"));
            }
            else if (instanceToCancel != null)
            {
                _logger.LogInformation("La instancia del evento con ID de instancia {instanceId} (del evento maestro {IdCalendario}) en la fecha {fechaOriginal} ya estaba cancelada.", instanceToCancel.Id, sala.IdCalendario, fechaOriginal.ToString("yyyy-MM-dd"));
            }
            else
            {
                _logger.LogWarning("No se encontró una instancia del evento {IdCalendario} en la fecha {fechaOriginal} para cancelar. Se continuará con la creación del nuevo evento.", sala.IdCalendario, fechaOriginal.ToString("yyyy-MM-dd"));
            }

            // 3. Crear un nuevo evento único para la nueva fecha
            var horaInicio = recurringEvent.Start?.DateTimeDateTimeOffset?.TimeOfDay ?? TimeSpan.Zero;
            var horaTermino = recurringEvent.End?.DateTimeDateTimeOffset?.TimeOfDay ?? TimeSpan.Zero;
            var timeZone = recurringEvent.Start?.TimeZone;
            var startOffset = recurringEvent.Start?.DateTimeDateTimeOffset?.Offset ?? TimeSpan.Zero;
            var newStart = new DateTimeOffset(request.FechaNuevaSesion.Year, request.FechaNuevaSesion.Month, request.FechaNuevaSesion.Day, horaInicio.Hours, horaInicio.Minutes, horaInicio.Seconds, startOffset);
            var endOffset = recurringEvent.End?.DateTimeDateTimeOffset?.Offset ?? TimeSpan.Zero;
            var newEnd = new DateTimeOffset(request.FechaNuevaSesion.Year, request.FechaNuevaSesion.Month, request.FechaNuevaSesion.Day, horaTermino.Hours, horaTermino.Minutes, horaTermino.Seconds, endOffset);

            var newEvent = new Google.Apis.Calendar.v3.Data.Event
            {
                Summary = recurringEvent.Summary,
                Location = recurringEvent.Location,
                Description = recurringEvent.Description,
                Attendees = recurringEvent.Attendees,
                Reminders = recurringEvent.Reminders,
                Start = new Google.Apis.Calendar.v3.Data.EventDateTime { DateTimeDateTimeOffset = newStart, TimeZone = timeZone },
                End = new Google.Apis.Calendar.v3.Data.EventDateTime { DateTimeDateTimeOffset = newEnd, TimeZone = timeZone }
            };

            _logger.LogInformation("Attempting to create unique event. newStart: {NewStart}, newEnd: {NewEnd}, timeZone: {TimeZone}, attendeesCount: {AttendeesCount}",
                newStart, newEnd, timeZone, newEvent.Attendees?.Count ?? 0);

            string? newCalendarEventId = await _calendarService.CrearEventoUnicoAsync(newEvent, sendNotifications: false);
            if (string.IsNullOrEmpty(newCalendarEventId))
            {
                _logger.LogError("Fallo al crear el nuevo evento único para la reprogramación.");
                return false;
            }
            _logger.LogInformation("Nueva sesión única creada con ID {newCalendarEventId} para el evento maestro {IdCalendario} en la fecha {nuevaFecha}.", newCalendarEventId, sala.IdCalendario, request.FechaNuevaSesion.ToString("yyyy-MM-dd"));
                    _logger.LogInformation("El evento de calendario con ID {IdCalendario} fue actualizado para reflejar la reprogramación de la sesión. Nuevo ID de evento: {NewCalendarEventId}", sala.IdCalendario, newCalendarEventId);
            
                    // Update the session in the database with the new calendar event ID
                    DateOnly nuevaFecha = request.FechaNuevaSesion;
                    bool dbUpdateSuccess = await _cursoRepository.ReprogramarSesionAsync(idCursoAbierto, request.SesionNumero, nuevaFecha, newCalendarEventId);
                    if (!dbUpdateSuccess)
                    {
                        _logger.LogError("No se pudo actualizar la sesión en la base de datos con el nuevo ID de calendario.");
                        return false;
                    }
            var alumnos = await _cursoRepository.ObtenerAlumnosConCorreosPorCursoAsync(idCursoAbierto);
            if (alumnos.Any())
            {
                var emailSubject = $"Reprogramación de clase: {sala.NombreSala}";
                var emailBody = EmailTemplate.GetReprogramacionSesionBody(
                    sala.NombreSala ?? "Nombre de sala no disponible",
                    fechaOriginal.ToString("dd-MM-yyyy"),
                    request.FechaNuevaSesion.ToString("dd-MM-yyyy"),
                    sala.UrlSala ?? "URL no disponible",
                    sala.ClaveEspectador ?? "No disponible");

                var alumnosConEmailValido = alumnos.Where(a => !string.IsNullOrEmpty(a.Email)).ToList();
                foreach (var alumno in alumnosConEmailValido)
                {
                    await _emailService.EnviarCorreoSimpleAsync(alumno.Email, emailSubject, emailBody);
                }
                _logger.LogInformation("Correos de reprogramación enviados a {count} alumnos.", alumnosConEmailValido.Count);
            }
            return true; // All operations successful
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reprogramar la sesión para el curso {IdCursoAbierto}. Detalles: {ErrorMessage}", request.IdCursoAbierto, ex.Message);
            return false; // An error occurred
        }
    }

    public async Task SincronizarCalendarioAsync(int idCursoAbierto)
    {
        _logger.LogInformation("Iniciando sincronización de calendario para el curso {IdCursoAbierto}.", idCursoAbierto);

        var sala = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(idCursoAbierto);
        if (sala == null || string.IsNullOrEmpty(sala.IdCalendario))
        {
            _logger.LogWarning("No se puede sincronizar: El curso {IdCursoAbierto} no existe o no tiene un calendario asociado.", idCursoAbierto);
            return;
        }

        var sesionesActivas = await _cursoRepository.ObtenerSesionesActivasPorCursoAsync(idCursoAbierto);
        var fechasSesionesActivas = new HashSet<DateOnly>(sesionesActivas.Where(s => s.Fecha.HasValue).Select(s => s.Fecha!.Value));

        _logger.LogInformation("Se encontraron {Count} sesiones activas en la base de datos.", fechasSesionesActivas.Count);

        var instanciasCalendario = await _calendarService.ObtenerInstanciasDeEventoAsync(sala.IdCalendario);
        _logger.LogInformation("Se encontraron {Count} instancias de eventos en el calendario de Google.", instanciasCalendario.Count);

        int eventosCancelados = 0;
        foreach (var instancia in instanciasCalendario)
        {
            if (instancia.Start?.DateTimeDateTimeOffset == null)
            {
                _logger.LogWarning("La instancia de evento con ID {Id} no tiene fecha de inicio y será omitida.", instancia.Id);
                continue;
            }

            var fechaInstancia = DateOnly.FromDateTime(instancia.Start.DateTimeDateTimeOffset.Value.Date);

            bool esSesionActiva = fechasSesionesActivas.Contains(fechaInstancia);

            if (!esSesionActiva)
            {
                if (instancia.Status != "cancelled")
                {
                    try
                    {
                        await _calendarService.CancelarInstanciaAsync(instancia, sendNotifications: false);
                        _logger.LogInformation("Instancia de evento {Id} en fecha {Fecha} cancelada. Motivo: No corresponde a una sesión activa.",
                            instancia.Id, fechaInstancia.ToString("yyyy-MM-dd"));
                        eventosCancelados++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error al cancelar la instancia de evento {Id} en fecha {Fecha}.", instancia.Id, fechaInstancia.ToString("yyyy-MM-dd"));
                    }
                }
                else
                {
                    _logger.LogInformation("La instancia de evento {Id} en fecha {Fecha} ya se encontraba cancelada.", instancia.Id, fechaInstancia.ToString("yyyy-MM-dd"));
                }
            }
        }

        _logger.LogInformation("Sincronización de calendario para el curso {IdCursoAbierto} completada. Se cancelaron {Count} eventos.", idCursoAbierto, eventosCancelados);
    }
}