using bbbAPIGL.DTOs;
using bbbAPIGL.Models;
using bbbAPIGL.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bbbAPIGL.Services;

public class SalaService : ISalaService
{
    // 1. Declaración de las variables privadas que usará la clase
    private readonly ISalaRepository _salaRepository;
    private readonly ICursoRepository _cursoRepository;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;

    // 2. Constructor explícito que recibe las dependencias
    public SalaService(
        ISalaRepository salaRepository,
        ICursoRepository cursoRepository,
        IConfiguration configuration,
        IEmailService emailService)
    {
        // 3. Asignación de los parámetros a las variables privadas
        _salaRepository = salaRepository;
        _cursoRepository = cursoRepository;
        _configuration = configuration;
        _emailService = emailService;
    }

    public async Task<CrearSalaResponse> CrearNuevaSalaAsync(CrearSalaRequest request)
    {
        var meetingId = GeneraMeetingId();
        var friendlyId = GeneraFriendlyId();
        var claveModerador = GeneraRandomPassword(8);
        var claveEspectador = GeneraRandomPassword(8);
        var recordId = $"{meetingId}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var baseUrl = _configuration["SalaSettings:BaseUrl"];

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
            UrlSala = $"{baseUrl}/rooms/{nuevaSala.FriendlyId}/join",
            ClaveModerador = nuevaSala.ClaveModerador,
            ClaveEspectador = nuevaSala.ClaveEspectador,
            MeetingId = nuevaSala.MeetingId,
            FriendlyId = nuevaSala.FriendlyId,
            RecordId = recordId
        };

        // Si la petición incluye una lista de participantes, envía las invitaciones.
        //if (request.CorreosParticipantes != null && request.CorreosParticipantes.Any())
        //{
        //    // Se ejecuta en segundo plano para no hacer esperar al usuario
        //    _ = Task.Run(() => _emailService.EnviarInvitacionCalendarioAsync(apiResponse, request.CorreosParticipantes));
        //}

        return apiResponse;
    }

    public async Task<bool> EliminarSalaAsync(Guid roomId)
    {
        return await _salaRepository.EliminarSalaAsync(roomId);
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

        var asunto = $"Invitación a la sala: {sala.NombreSala}";
        var cuerpoHtml = $"<p>Hola,</p><p>Has sido invitado a unirte a la sala virtual '<strong>{sala.NombreSala}</strong>'.</p>" +
                         $"<p>Puedes unirte aqui: <a href='{sala.UrlSala}'>{sala.UrlSala}</a></p>" +
                         $"<p>Clave de Espectador: <strong>{sala.ClaveEspectador}</strong></p>";

        await _emailService.EnviarCorreosAsync(correos, asunto, cuerpoHtml);

        return new EnviarInvitacionCursoResponse
        {
            Mensaje = "Invitaciones enviadas exitosamente.",
            CorreosEnviados = correos.Count
        };
    }

    // --- Métodos de Ayuda ---
    private static string GeneraMeetingId()
    {
        return Guid.NewGuid().ToString();
    }

    private static string GeneraFriendlyId()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var part1 = new string(Enumerable.Repeat(chars, 3).Select(s => s[random.Next(s.Length)]).ToArray());
        var part2 = new string(Enumerable.Repeat(chars, 3).Select(s => s[random.Next(s.Length)]).ToArray());
        var part3 = new string(Enumerable.Repeat(chars, 3).Select(s => s[random.Next(s.Length)]).ToArray());
        var part4 = new string(Enumerable.Repeat(chars, 3).Select(s => s[random.Next(s.Length)]).ToArray());
        return $"{part1}-{part2}-{part3}-{part4}";
    }

    private static string GeneraRandomPassword(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
    }
    public async Task<List<GrabacionDto>?> ObtenerUrlsGrabacionesAsync(int idCursoAbierto)
    {
        // Paso 1: Obtener el RoomId desde MySQL (esto no cambia)
        var curso = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(idCursoAbierto);
        if (curso == null || !Guid.TryParse(curso.RoomId, out var roomId))
        {
            return null; 
        }

        // Paso 2: Obtener TODAS las grabaciones para ese RoomId desde PostgreSQL
        var recordingInfos = await _salaRepository.ObtenerTodosLosRecordIdsPorRoomIdAsync(roomId);

        if (recordingInfos == null || !recordingInfos.Any())
        {
            return new List<GrabacionDto>();
        }

        // Paso 3: Construir la lista de URLs de reproducción
        var baseUrl = _configuration["SalaSettings:BaseUrl"];
        
        var grabacionesDto = recordingInfos.Select(rec => new GrabacionDto
        {
            RecordId = rec.RecordId,
            CreatedAt = rec.CreatedAt.ToString("yyyy-MM-dd"),
            PlaybackUrl = $"{baseUrl}/playback/presentation/2.3/{rec.RecordId}"
        }).ToList();

        return grabacionesDto;
    }
}