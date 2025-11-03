using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using bbbAPIGL.Models;

/// <summary>
/// Implementación del repositorio de cursos utilizando MySQL.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 16-10-2025
/// </summary>
namespace bbbAPIGL.Repositories;

public class MySqlCursoRepository : ICursoRepository
{
    private readonly string _connectionString;
    private readonly ILogger<MySqlCursoRepository> _logger;

    public MySqlCursoRepository(IConfiguration configuration, ILogger<MySqlCursoRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("MySqlDb")!;
        _logger = logger;
    }
    
    /// <summary>
    /// Obtiene una lista de correos electrónicos de los alumnos inscritos en un curso desde la base de datos MySQL.
    /// </summary>
    /// <param name="idCurso">El ID del curso.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es una lista de correos electrónicos.</returns>
    public async Task<List<string>> ObtenerCorreosPorCursoAsync(string idCurso)
    {
        try
        {
            var emails = new List<string>();
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT alu.Email 
                FROM sige_sam_v3.detallecontrato as detcon 
                INNER JOIN alumnos as alu ON detcon.idAlumno = alu.idAlumno 
                WHERE detcon.Activo = 1 AND detcon.idCursoAbierto = @IdCurso";
            
            await using var command = new MySqlCommand(sql, connection);
            
            command.Parameters.AddWithValue("@IdCurso", idCurso);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                emails.Add(reader.GetString("Email"));
            }

            return emails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener correos por curso: {IdCurso}", idCurso);
            throw;
        }
    }

    /// <summary>
    /// Obtiene una lista de tuplas (IdAlumno, Email) para un curso abierto específico desde la base de datos MySQL.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es una lista de tuplas con el ID del alumno y su correo electrónico.</returns>
    public async Task<List<(string IdAlumno, string Email)>> ObtenerAlumnosConCorreosPorCursoAsync(int idCursoAbierto)
    {
        try
        {
            var alumnos = new List<(string IdAlumno, string Email)>();
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT alu.idAlumno, alu.Email 
                FROM sige_sam_v3.detallecontrato as detcon 
                INNER JOIN alumnos as alu ON detcon.idAlumno = alu.idAlumno 
                WHERE detcon.Activo = 1 AND detcon.idCursoAbierto = @IdCursoAbierto";
            
            await using var command = new MySqlCommand(sql, connection);
            
            command.Parameters.AddWithValue("@IdCursoAbierto", idCursoAbierto);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                alumnos.Add((reader.GetString("idAlumno"), reader.GetString("Email")));
            }

            return alumnos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener alumnos con correos por curso: {IdCursoAbierto}", idCursoAbierto);
            throw;
        }
    }

    /// <summary>
    /// Obtiene los datos de la sala asociados a un curso abierto desde la base de datos MySQL.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es un objeto CursoAbiertoSala o null si no se encuentra.</returns>
    public async Task<CursoAbiertoSala?> ObtenerDatosSalaPorCursoAsync(int idCursoAbierto)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();
            
            string sql = @"
                SELECT 
                    idCursoAbierto, roomId, urlSala, claveModerador, claveEspectador, 
                    meetingId, friendlyId, recordId, nombreSala, idCalendario,
                    fechaInicio, fechaTermino, dias, horaInicio, horaTermino 
                FROM cursosabiertosbbb 
                WHERE idCursoAbierto = @IdCursoAbierto";
            
            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdCursoAbierto", idCursoAbierto);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new CursoAbiertoSala
                {
                    IdCursoAbierto = reader.GetInt32("idCursoAbierto"),
                    RoomId = reader.IsDBNull(reader.GetOrdinal("roomId")) ? null : reader.GetString("roomId"),
                    UrlSala = reader.IsDBNull(reader.GetOrdinal("urlSala")) ? null : reader.GetString("urlSala"),
                    ClaveModerador = reader.IsDBNull(reader.GetOrdinal("claveModerador")) ? null : reader.GetString("claveModerador"),
                    ClaveEspectador = reader.IsDBNull(reader.GetOrdinal("claveEspectador")) ? null : reader.GetString("claveEspectador"),
                    MeetingId = reader.IsDBNull(reader.GetOrdinal("meetingId")) ? null : reader.GetString("meetingId"),
                    FriendlyId = reader.IsDBNull(reader.GetOrdinal("friendlyId")) ? null : reader.GetString("friendlyId"),
                    RecordId = reader.IsDBNull(reader.GetOrdinal("recordId")) ? null : reader.GetString("recordId"),
                    NombreSala = reader.IsDBNull(reader.GetOrdinal("nombreSala")) ? null : reader.GetString("nombreSala"),
                    IdCalendario = reader.IsDBNull(reader.GetOrdinal("idCalendario")) ? null : reader.GetString("idCalendario"),
                    FechaInicio = reader.IsDBNull(reader.GetOrdinal("fechaInicio")) ? default : reader.GetDateTime("fechaInicio"),
                    FechaTermino = reader.IsDBNull(reader.GetOrdinal("fechaTermino")) ? default : reader.GetDateTime("fechaTermino"),
                    Dias = reader.IsDBNull(reader.GetOrdinal("dias")) ? null : reader.GetString("dias"),
                    HoraInicio = reader.IsDBNull(reader.GetOrdinal("horaInicio")) ? default : reader.GetDateTime("horaInicio"),
                    HoraTermino = reader.IsDBNull(reader.GetOrdinal("horaTermino")) ? default : reader.GetDateTime("horaTermino")
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener datos de sala por curso: {IdCursoAbierto}", idCursoAbierto);
            throw;
        }
    }

    /// <summary>
    /// Desasocia una sala de todos los cursos a los que está vinculada en la base de datos MySQL.
    /// </summary>
    /// <param name="roomId">El ID de la sala a desasociar.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es true si se desasoció con éxito, false en caso contrario.</returns>
    public async Task<bool> DesasociarSalaDeCursosAsync(Guid roomId)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                UPDATE cursosabiertosbbb 
                SET 
                    roomId = NULL, 
                    urlSala = NULL, 
                    claveModerador = NULL, 
                    claveEspectador = NULL, 
                    meetingId = NULL, 
                    friendlyId = NULL,
                    recordId = NULL,
                    nombreSala = NULL,
                    idCalendario = NULL
                WHERE roomId = @RoomId";

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@RoomId", roomId.ToString());

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al desasociar sala de cursos: {RoomId}", roomId);
            throw;
        }
    }

    /// <summary>
    /// Actualiza el ID del calendario asociado a un curso abierto en la base de datos MySQL.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <param name="idCalendario">El nuevo ID del calendario.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task ActualizarIdCalendarioCursoAsync(int idCursoAbierto, string idCalendario)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                UPDATE cursosabiertosbbb 
                SET idCalendario = @IdCalendario 
                WHERE idCursoAbierto = @IdCursoAbierto";

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdCalendario", idCalendario);
            command.Parameters.AddWithValue("@IdCursoAbierto", idCursoAbierto);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar id de calendario de curso: {IdCursoAbierto}", idCursoAbierto);
            throw;
        }
    }

    /// <summary>
    /// Guarda una invitación de curso abierto en la base de datos MySQL.
    /// </summary>
    /// <param name="invitacion">El objeto CursoAbiertoInvitacion a guardar.</param>
    /// <returns>Una tarea que representa la operación asíncrona.</returns>
    public async Task GuardarInvitacionAsync(CursoAbiertoInvitacion invitacion)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                INSERT INTO cursosabiertosbbbinvitaciones 
                (idCursoAbiertoBbb, idAlumno, url, idCalendario, fechaCreacion) 
                VALUES (@IdCursoAbiertoBbb, @IdAlumno, @Url, @IdCalendario, @FechaCreacion)";

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdCursoAbiertoBbb", invitacion.IdCursoAbiertoBbb);
            command.Parameters.AddWithValue("@IdAlumno", invitacion.IdAlumno);
            command.Parameters.AddWithValue("@Url", invitacion.Url);
            command.Parameters.AddWithValue("@IdCalendario", invitacion.IdCalendario);
            command.Parameters.AddWithValue("@FechaCreacion", invitacion.FechaCreacion);

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar invitación.");
            throw;
        }
    }

    /// <summary>
    /// Obtiene una invitación de curso abierto por el ID del curso y el ID del alumno desde la base de datos MySQL.
    /// </summary>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <param name="idAlumno">El ID del alumno.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es un objeto CursoAbiertoInvitacion o null si no se encuentra.</returns>
    public async Task<CursoAbiertoInvitacion?> ObtenerInvitacionPorCursoAlumnoAsync(int idCursoAbierto, string idAlumno)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT id, idCursoAbiertoBbb, idAlumno, url, idCalendario, fechaCreacion 
                FROM cursosabiertosbbbinvitaciones 
                WHERE idCursoAbiertoBbb = @IdCursoAbiertoBbb AND idAlumno = @IdAlumno";

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdCursoAbiertoBbb", idCursoAbierto);
            command.Parameters.AddWithValue("@IdAlumno", idAlumno);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new CursoAbiertoInvitacion
                {
                    Id = reader.GetInt32("id"),
                    IdCursoAbiertoBbb = reader.GetInt32("idCursoAbiertoBbb"),
                    IdAlumno = reader.GetString("idAlumno"),
                    Url = reader.GetString("url"),
                    IdCalendario = reader.IsDBNull(reader.GetOrdinal("idCalendario")) ? null : reader.GetString("idCalendario"),
                    FechaCreacion = reader.GetDateTime("fechaCreacion")
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener invitación por curso y alumno: {IdCursoAbierto}, {IdAlumno}", idCursoAbierto, idAlumno);
            throw;
        }
    }

    /// <summary>
    /// Obtiene el correo electrónico de un alumno específico para un curso abierto desde la base de datos MySQL.
    /// </summary>
    /// <param name="idAlumno">El ID del alumno.</param>
    /// <param name="idCursoAbierto">El ID del curso abierto.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es el correo electrónico del alumno o null si no se encuentra.</returns>
    public async Task<string?> ObtenerCorreoPorAlumnoAsync(string idAlumno, int idCursoAbierto)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT alu.Email 
                FROM sige_sam_v3.detallecontrato as detcon 
                INNER JOIN alumnos as alu ON detcon.idAlumno = alu.idAlumno 
                WHERE detcon.Activo = 1 AND detcon.idCursoAbierto = @IdCursoAbierto AND alu.idAlumno = @IdAlumno";
        
            await using var command = new MySqlCommand(sql, connection);
        
            command.Parameters.AddWithValue("@IdCursoAbierto", idCursoAbierto);
            command.Parameters.AddWithValue("@IdAlumno", idAlumno);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener correo por alumno: {IdAlumno}, {IdCursoAbierto}", idAlumno, idCursoAbierto);
            throw;
        }
    }
}
