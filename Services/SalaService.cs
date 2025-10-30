using bbbAPIGL.DTOs;
using bbbAPIGL.Models;
using bbbAPIGL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bbbAPIGL.Services;

public class SalaService : ISalaService
{
    private readonly ISalaRepository _salaRepository;
    private readonly ICursoRepository _cursoRepository;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<SalaService> _logger;

    public SalaService(
        ISalaRepository salaRepository,
        ICursoRepository cursoRepository,
        IConfiguration configuration,
        IEmailService emailService,
        ILogger<SalaService> logger)
    {
        _salaRepository = salaRepository;
        _cursoRepository = cursoRepository;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

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

        // Si la petición incluye una lista de participantes, envía las invitaciones y guarda el ID del evento.
        if (request.CorreosParticipantes != null && request.CorreosParticipantes.Any())
        {
            var apiResponseForCalendar = new CrearSalaResponse
            {
                NombreSala = nuevaSala.Nombre,
                UrlSala = $"{publicUrl}/rooms/{friendlyId}/join",
                ClaveModerador = claveModerador,
                ClaveEspectador = claveEspectador,
                MeetingId = meetingId,
                FriendlyId = friendlyId
            };
            try
            {
                nuevaSala.IdCalendario = await _emailService.EnviarInvitacionCalendarioAsync(apiResponseForCalendar, request.CorreosParticipantes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar invitación de calendario durante la creación de la sala. La sala se creará sin evento de calendario.");
                nuevaSala.IdCalendario = null; // Asegurarse de que no se guarde un ID parcial o erróneo.
            }
        }

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
            RecordId = recordId
        };

        return apiResponse;
    }

    public async Task<bool> EliminarSalaAsync(Guid roomId)
    {
        var idCalendario = await _salaRepository.ObtenerIdCalendarioPorSalaIdAsync(roomId);

        if (!string.IsNullOrEmpty(idCalendario))
        {
            try
            {
                await _emailService.EliminarEventoCalendarioAsync(idCalendario);
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

    public async Task<EnviarInvitacionCursoResponse> EnviarInvitacionesCursoAsync(EnviarInvitacionCursoRequest request)
    {
        var sala = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(request.IdCursoAbierto);
        if (sala == null)
        {
            throw new InvalidOperationException("El curso abierto especificado no fue encontrado.");
        }

        var correos = await _cursoRepository.ObtenerCorreosPorCursoAsync(request.IdCursoAbierto.ToString());
        if (correos == null || !correos.Any())
        {
            return new EnviarInvitacionCursoResponse { Mensaje = "No se encontraron alumnos para el curso.", CorreosEnviados = 0 };
        }

        var detallesSalaParaInvitacion = new CrearSalaResponse
        {
            RoomId = Guid.TryParse(sala.RoomId, out var roomId) ? roomId : Guid.Empty, 
            NombreSala = sala.NombreSala,
            FriendlyId = sala.FriendlyId ?? string.Empty,
            UrlSala = sala.UrlSala ?? string.Empty,
            ClaveEspectador = sala.ClaveEspectador ?? string.Empty,
            ClaveModerador = sala.ClaveModerador ?? string.Empty,
            MeetingId = sala.MeetingId ?? string.Empty,
            RecordId = string.Empty
        };

        if (sala.FechaInicio == default || sala.FechaTermino == default || string.IsNullOrEmpty(sala.Dias))
        {
            throw new InvalidOperationException("El curso no tiene un horario definido (fechas o días de la semana) para crear un evento recurrente.");
        }

        try
        {
            await _emailService.EnviarInvitacionCalendarioAsync(detallesSalaParaInvitacion, correos, sala.FechaInicio, sala.FechaTermino, sala.Dias, sala.HoraInicio, sala.HoraTermino);
        }
        catch (Exception ex)
        {
            // Aquí podrías loggear el error ex para tener más detalles.
            // Por ejemplo: _logger.LogError(ex, "Error al enviar invitaciones de calendario.");
            throw new InvalidOperationException("Error al procesar la invitación con el servicio de calendario. Verifica la configuración y los permisos.", ex);
        }

        return new EnviarInvitacionCursoResponse
        {
            Mensaje = "Invitaciones enviadas exitosamente.",
            CorreosEnviados = correos.Count
        };
    }

    private static string GeneraMeetingId()
    {
        return Guid.NewGuid().ToString();
    }

    private static string GeneraFriendlyId()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var part1 = new string(Enumerable.Repeat(chars, 3).Select(s => s[Random.Shared.Next(s.Length)]).ToArray()); // Usa Random.Shared
        var part2 = new string(Enumerable.Repeat(chars, 3).Select(s => s[Random.Shared.Next(s.Length)]).ToArray()); // Usa Random.Shared
        var part3 = new string(Enumerable.Repeat(chars, 3).Select(s => s[Random.Shared.Next(s.Length)]).ToArray()); // Usa Random.Shared
        var part4 = new string(Enumerable.Repeat(chars, 3).Select(s => s[Random.Shared.Next(s.Length)]).ToArray()); // Usa Random.Shared
        return $"{part1}-{part2}-{part3}-{part4}";
    }

    private static string GeneraRandomPassword(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Shared.Next(s.Length)]).ToArray()); // Usa Random.Shared
    }
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

    public async Task<EnviarInvitacionCursoResponse> EnviarInvitacionIndividualAsync(EnviarInvitacionIndividualRequest request)
    {
        var sala = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(request.IdCursoAbierto);
        if (sala == null)
        {
            throw new InvalidOperationException("El curso abierto especificado no fue encontrado.");
        }

        var correo = await _cursoRepository.ObtenerCorreoPorAlumnoAsync(request.IdAlumno, request.IdCursoAbierto);
        if (string.IsNullOrEmpty(correo))
        {
            return new EnviarInvitacionCursoResponse { Mensaje = "No se encontró un alumno con el ID especificado para este curso.", CorreosEnviados = 0 };
        }

        var detallesSalaParaInvitacion = new CrearSalaResponse
        {
            RoomId = Guid.TryParse(sala.RoomId, out var roomId) ? roomId : Guid.Empty,
            NombreSala = sala.NombreSala,
            FriendlyId = sala.FriendlyId ?? string.Empty,
            UrlSala = sala.UrlSala ?? string.Empty,
            ClaveEspectador = sala.ClaveEspectador ?? string.Empty,
            ClaveModerador = sala.ClaveModerador ?? string.Empty,
            MeetingId = sala.MeetingId ?? string.Empty,
            RecordId = string.Empty
        };

        if (sala.FechaInicio == default || sala.FechaTermino == default || string.IsNullOrEmpty(sala.Dias))
        {
            throw new InvalidOperationException("El curso no tiene un horario definido (fechas o días de la semana) para crear un evento recurrente.");
        }

        try
        {
            await _emailService.EnviarInvitacionCalendarioAsync(detallesSalaParaInvitacion, new List<string> { correo }, sala.FechaInicio, sala.FechaTermino, sala.Dias, sala.HoraInicio, sala.HoraTermino);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al procesar la invitación individual con el servicio de calendario.");
            throw new InvalidOperationException("Error al procesar la invitación con el servicio de calendario. Verifica la configuración y los permisos.", ex);
        }

        return new EnviarInvitacionCursoResponse
        {
            Mensaje = "Invitación enviada exitosamente.",
            CorreosEnviados = 1
        };
    }

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

        var detallesSalaParaInvitacion = new CrearSalaResponse
        {
            RoomId = Guid.TryParse(sala.RoomId, out var roomId) ? roomId : Guid.Empty,
            NombreSala = sala.NombreSala,
            FriendlyId = sala.FriendlyId ?? string.Empty,
            UrlSala = sala.UrlSala ?? string.Empty,
            ClaveEspectador = sala.ClaveEspectador ?? string.Empty,
            ClaveModerador = sala.ClaveModerador ?? string.Empty,
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
            await _emailService.ActualizarEventoCalendarioAsync(sala.IdCalendario, detallesSalaParaInvitacion, correos, fechaInicio, fechaTermino, dias, horaInicio, horaTermino);
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
}