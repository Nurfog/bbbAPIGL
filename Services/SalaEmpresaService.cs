using bbbAPIGL.DTOs;
using bbbAPIGL.Models;
using bbbAPIGL.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bbbAPIGL.Utils;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace bbbAPIGL.Services;

public class SalaEmpresaService : ISalaEmpresaService
{
    private readonly ISalaRepository _salaRepository;
    private readonly ICursoEmpresaRepository _cursoRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SalaEmpresaService> _logger;

    public SalaEmpresaService(
        ISalaRepository salaRepository,
        ICursoEmpresaRepository cursoRepository,
        IConfiguration configuration,
        ILogger<SalaEmpresaService> logger)
    {
        _salaRepository = salaRepository;
        _cursoRepository = cursoRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<CrearSalaResponse> CrearNuevaSalaAsync(CrearSalaRequest request)
    {
        var salaExistente = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(request.IdCursoAbierto);
        if (salaExistente != null && !string.IsNullOrEmpty(salaExistente.RoomId))
        {
            _logger.LogInformation("Ya existe una sala para el curso EMP {IdCursoAbierto}. Retornando datos existentes.", request.IdCursoAbierto);
            return new CrearSalaResponse
            {
                RoomId = Guid.TryParse(salaExistente.RoomId, out var existingGuid) ? existingGuid : Guid.Empty,
                NombreSala = salaExistente.NombreSala ?? request.Nombre,
                UrlSala = salaExistente.UrlSala ?? string.Empty,
                ClaveModerador = salaExistente.ClaveModerador ?? string.Empty,
                ClaveEspectador = salaExistente.ClaveEspectador ?? string.Empty,
                MeetingId = salaExistente.MeetingId ?? string.Empty,
                RecordId = salaExistente.RecordId ?? string.Empty,
                FriendlyId = salaExistente.FriendlyId ?? string.Empty
            };
        }

        var meetingId = Guid.NewGuid().ToString();
        var friendlyId = string.Join("-", Enumerable.Range(0, 4).Select(_ => GeneraRandomPassword(3)));
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

        if (newRoomId == null)
        {
            throw new ApplicationException("No se pudo crear la sala en la base de datos.");
        }

        var apiResponse = new CrearSalaResponse
        {
            RoomId = newRoomId.Value,
            NombreSala = nuevaSala.Nombre,
            UrlSala = string.IsNullOrEmpty(publicUrl) ? string.Empty : $"{publicUrl}/rooms/{nuevaSala.FriendlyId}/join",
            ClaveModerador = nuevaSala.ClaveModerador,
            ClaveEspectador = nuevaSala.ClaveEspectador,
            MeetingId = nuevaSala.MeetingId,
            RecordId = recordId,
            FriendlyId = nuevaSala.FriendlyId
        };

        try
        {
            var cursoSala = new CursoAbiertoSala
            {
                IdCursoAbierto = request.IdCursoAbierto,
                RoomId = apiResponse.RoomId.ToString(),
                UrlSala = apiResponse.UrlSala,
                ClaveModerador = apiResponse.ClaveModerador,
                ClaveEspectador = apiResponse.ClaveEspectador,
                MeetingId = apiResponse.MeetingId,
                FriendlyId = apiResponse.FriendlyId,
                RecordId = apiResponse.RecordId,
                NombreSala = apiResponse.NombreSala
            };

            await _cursoRepository.GuardarDatosSalaEnCursoAsync(cursoSala);
            await _cursoRepository.ActualizarHorarioDesdeFuenteExternaAsync(request.IdCursoAbierto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al vincular o sincronizar el curso EMP {IdCursoAbierto} en MySQL.", request.IdCursoAbierto);
        }

        return apiResponse;
    }

    public async Task<bool> EliminarSalaAsync(Guid roomId)
    {
        var exitoPostgres = await _salaRepository.EliminarSalaAsync(roomId);
        if (exitoPostgres)
        {
            await _cursoRepository.DesasociarSalaDeCursosAsync(roomId);
        }
        return exitoPostgres;
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
        return recordingInfos.Select(rec => new GrabacionDto
        {
            RecordId = rec.RecordId,
            CreatedAt = rec.CreatedAt.ToString("yyyy-MM-dd"),
            PlaybackUrl = $"{publicUrl}/playback/presentation/2.3/{rec.RecordId}"
        }).ToList();
    }

    public async Task<bool> EliminarCursoAsync(int idCursoAbierto)
    {
        return await _cursoRepository.EliminarCursoAsync(idCursoAbierto);
    }

    public async Task<SalaStatusDto> ObtenerEstadoSalaAsync(int idCursoAbierto)
    {
        var status = new SalaStatusDto { ExisteEnSam = false, ExisteEnGreenlight = false, EstaActivaEnBBB = false };
        var cursoSam = await _cursoRepository.ObtenerDatosSalaPorCursoAsync(idCursoAbierto);
        
        if (cursoSam != null && !string.IsNullOrEmpty(cursoSam.RoomId))
        {
            status.ExisteEnSam = true;
            status.UrlSala = cursoSam.UrlSala;
            
            if (Guid.TryParse(cursoSam.RoomId, out var roomId))
            {
                var salaGl = await _salaRepository.ObtenerSalaPorIdAsync(roomId);
                if (salaGl != null)
                {
                    status.ExisteEnGreenlight = true;
                    status.EstaActivaEnBBB = await IsMeetingRunningAsync(cursoSam.MeetingId);
                }
            }
        }
        
        return status;
    }

    public async Task<bool> ReprogramarSesionAsync(ReprogramarSesionRequest request)
    {
        return await _cursoRepository.ReprogramarSesionAsync(request.IdCursoAbierto, request.SesionNumero, request.FechaNuevaSesion, null);
    }

    private async Task<bool> IsMeetingRunningAsync(string? meetingId)
    {
        if (string.IsNullOrEmpty(meetingId)) return false;
        try
        {
            var baseUrl = _configuration["BigBlueButtonApi:BaseUrl"];
            var secret = _configuration["BigBlueButtonApi:Secret"];
            var query = $"meetingID={meetingId}";
            var checksum = ComputeSha1( $"isMeetingRunning{query}{secret}");
            var url = $"{baseUrl}/isMeetingRunning?{query}&checksum={checksum}";

            using var client = new HttpClient();
            var response = await client.GetStringAsync(url);
            return response.Contains("<running>true</running>");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al verificar si la reunión {MeetingId} está activa en BBB.", meetingId);
            return false;
        }
    }

    private static string ComputeSha1(string input)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    private static string GeneraRandomPassword(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}
