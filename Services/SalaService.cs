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
        if (request.CorreosParticipantes != null && request.CorreosParticipantes.Any())
        {
            // Se ejecuta en segundo plano para no hacer esperar al usuario
            // Usamos el overload sin fechas para invitaciones no recurrentes al crear una sala.
            _ = Task.Run(() => _emailService.EnviarInvitacionCalendarioAsync(apiResponse, request.CorreosParticipantes));
        }

        return apiResponse;
    }

    public async Task<bool> EliminarSalaAsync(Guid roomId)
    {
        // Primero, elimina la sala y todas sus dependencias de la base de datos de Greenlight (PostgreSQL).
        var exitoPostgres = await _salaRepository.EliminarSalaAsync(roomId);

        if (exitoPostgres)
        {
            // Si la eliminación en PostgreSQL fue exitosa, procede a desasociar la sala de
            // cualquier curso en la base de datos MySQL para mantener la consistencia.
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

        // Creamos un objeto con los detalles de la sala para enviar al servicio de calendario.
        // Esto nos permite reutilizar la lógica existente.
        var detallesSalaParaInvitacion = new CrearSalaResponse
        {
            // El RoomId no es estrictamente necesario para la invitación, pero lo incluimos por consistencia.
            RoomId = Guid.TryParse(sala.RoomId, out var roomId) ? roomId : Guid.Empty,
            FriendlyId = sala.NombreSala ?? string.Empty, // Usamos el nombre de la sala como identificador amigable en el evento.
            UrlSala = sala.UrlSala ?? string.Empty,
            ClaveEspectador = sala.ClaveEspectador ?? string.Empty,
            // Los siguientes campos no son relevantes para el cuerpo de la invitación, pero los inicializamos.
            ClaveModerador = string.Empty,
            MeetingId = string.Empty,
            RecordId = string.Empty
        };

        // --- CORRECCIÓN ---
        // La validación se mueve aquí para asegurar que 'sala' tiene los datos antes de usarlos.
        if (sala.FechaInicio == default || sala.FechaTermino == default || string.IsNullOrEmpty(sala.Dias))
        {
            throw new InvalidOperationException("El curso no tiene un horario definido (fechas o días de la semana) para crear un evento recurrente.");
        }

        // Se llama a la sobrecarga del servicio de email que maneja eventos recurrentes.
        await _emailService.EnviarInvitacionCalendarioAsync(detallesSalaParaInvitacion, correos, sala.FechaInicio, sala.FechaTermino, sala.Dias, sala.HoraInicio, sala.HoraTermino);

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