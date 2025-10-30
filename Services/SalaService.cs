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
/*
*/
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

        var alumnosConCorreos = await _cursoRepository.ObtenerAlumnosConCorreosPorCursoAsync(request.IdCursoAbierto);
        if (alumnosConCorreos == null || !alumnosConCorreos.Any())
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
                var cuerpoHtml = $"Hola,<br><br>Te recordamos tu clase de <b>{sala.NombreSala}</b>.<br>Puedes unirte a la sala virtual aquí: <a href=\"{invitacionExistente.Url}\">{invitacionExistente.Url}</a><br><br>Saludos.";
                await _emailService.EnviarCorreoSimpleAsync(alumno.Email, asunto, cuerpoHtml);
                correosEnviados++;
            }
            else
            {
                // Si no existe, enviar invitación de calendario y guardar
                try
                {
                    _logger.LogInformation("Enviando nueva invitación de calendario para el alumno {idAlumno}.", alumno.IdAlumno);
                    var idEventoCalendario = await _emailService.EnviarInvitacionCalendarioAsync(
                        detallesSala, 
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
                            Url = detallesSala.UrlSala,
                            IdCalendario = idEventoCalendario,
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

        var correoAlumno = await _cursoRepository.ObtenerCorreoPorAlumnoAsync(request.IdAlumno.ToString(), request.IdCursoAbierto);
        if (string.IsNullOrEmpty(correoAlumno))
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

        var invitacionExistente = await _cursoRepository.ObtenerInvitacionPorCursoAlumnoAsync(request.IdCursoAbierto, request.IdAlumno);

        if (invitacionExistente != null)
        {
            _logger.LogInformation("Invitación existente para el alumno {idAlumno}, enviando correo simple.", request.IdAlumno);
            var asunto = $"Recordatorio: Tu clase de {sala.NombreSala}";
            var cuerpoHtml = $"Hola,<br><br>Te recordamos tu clase de <b>{sala.NombreSala}</b>.<br>Puedes unirte a la sala virtual aquí: <a href=\"{invitacionExistente.Url}\">{invitacionExistente.Url}</a><br><br>Saludos.";
            await _emailService.EnviarCorreoSimpleAsync(correoAlumno, asunto, cuerpoHtml);
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
                var idEventoCalendario = await _emailService.EnviarInvitacionCalendarioAsync(
                    detallesSala, 
                    new List<string> { correoAlumno }, 
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
                        IdAlumno = request.IdAlumno,
                        Url = detallesSala.UrlSala,
                        IdCalendario = idEventoCalendario,
                        FechaCreacion = DateTime.UtcNow
                    };
                    await _cursoRepository.GuardarInvitacionAsync(nuevaInvitacion);

                    if (string.IsNullOrEmpty(sala.IdCalendario))
                    {
                        await _cursoRepository.ActualizarIdCalendarioCursoAsync(request.IdCursoAbierto, idEventoCalendario);
                    }
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