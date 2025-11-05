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
                    HoraInicio = reader.IsDBNull(reader.GetOrdinal("horaInicio")) ? default : reader.GetDateTime("fechaInicio").Date + reader.GetTimeSpan("horaInicio"),
                    HoraTermino = reader.IsDBNull(reader.GetOrdinal("horaTermino")) ? default : reader.GetDateTime("fechaInicio").Date + reader.GetTimeSpan("horaTermino")
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

            // First, try to update the existing invitation
            string updateSql = @"
                UPDATE cursosabiertosbbbinvitacion 
                SET url = @Url
                WHERE idcursosabiertosbbb = @IdCursoAbiertoBbb AND idAlumno = @IdAlumno";

            await using var updateCommand = new MySqlCommand(updateSql, connection);
            updateCommand.Parameters.AddWithValue("@Url", invitacion.Url);
            updateCommand.Parameters.AddWithValue("@IdCursoAbiertoBbb", invitacion.IdCursoAbiertoBbb);
            updateCommand.Parameters.AddWithValue("@IdAlumno", invitacion.IdAlumno);

            int rowsAffected = await updateCommand.ExecuteNonQueryAsync();

            // If no rows were updated, it means the invitation does not exist, so insert it
            if (rowsAffected == 0)
            {
                string insertSql = @"
                    INSERT INTO cursosabiertosbbbinvitacion 
                    (idcursosabiertosbbb, idAlumno, url) 
                    VALUES (@IdCursoAbiertoBbb, @IdAlumno, @Url)";

                await using var insertCommand = new MySqlCommand(insertSql, connection);
                insertCommand.Parameters.AddWithValue("@IdCursoAbiertoBbb", invitacion.IdCursoAbiertoBbb);
                insertCommand.Parameters.AddWithValue("@IdAlumno", invitacion.IdAlumno);
                insertCommand.Parameters.AddWithValue("@Url", invitacion.Url);

                await insertCommand.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al guardar invitación para curso {IdCursoAbiertoBbb} y alumno {IdAlumno}.", invitacion.IdCursoAbiertoBbb, invitacion.IdAlumno);
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
                SELECT idcursosabiertosbbb, idAlumno, url 
                FROM cursosabiertosbbbinvitacion 
                WHERE idcursosabiertosbbb = @IdCursoAbiertoBbb AND idAlumno = @IdAlumno";

            await using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@IdCursoAbiertoBbb", idCursoAbierto);
            command.Parameters.AddWithValue("@IdAlumno", idAlumno);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new CursoAbiertoInvitacion
                {
                    Id = 0, // Default value as 'id' column does not exist in DB
                    IdCursoAbiertoBbb = reader.GetInt32("idcursosabiertosbbb"),
                    IdAlumno = reader.GetString("idAlumno"),
                    Url = reader.GetString("url"),
                    IdCalendario = null,
                    FechaCreacion = default // Not retrieved from DB
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

    public async Task<List<CursoAbiertoInvitacion>> ObtenerInvitacionesPorCursoAsync(int idCursoAbierto)
    {
        var invitaciones = new List<CursoAbiertoInvitacion>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = @"
            SELECT idcursosabiertosbbb, idAlumno, url
            FROM cursosabiertosbbbinvitacion
            WHERE idcursosabiertosbbb = @IdCursoAbiertoBbb";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdCursoAbiertoBbb", idCursoAbierto);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            invitaciones.Add(new CursoAbiertoInvitacion
            {
                Id = 0, // Default value as 'id' column does not exist in DB
                IdCursoAbiertoBbb = reader.GetInt32("idcursosabiertosbbb"),
                IdAlumno = reader.GetString("idAlumno"),
                Url = reader.GetString("url"),
                IdCalendario = null,
                FechaCreacion = default // Not retrieved from DB
            });
        }

        return invitaciones;
    }

    public async Task<int> EliminarInvitacionesPorCursoAsync(int idCursoAbierto)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = "DELETE FROM cursosabiertosbbbinvitacion WHERE idcursosabiertosbbb = @IdCursoAbiertoBbb";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdCursoAbiertoBbb", idCursoAbierto);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> EliminarCursoAsync(int idCursoAbierto)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = "DELETE FROM cursosabiertosbbb WHERE idCursoAbierto = @IdCursoAbierto";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@IdCursoAbierto", idCursoAbierto);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<bool> ActualizarHorarioDesdeFuenteExternaAsync(int idCursoAbierto)
    {
        try
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            const string selectSql = @"
                SELECT a.idCursoAbierto,   
                       c.HoraFin,
                       b.nombrecorto,
                       c.Horainicio,
                       a.FechaInicio,
                       a.FechaFin
                FROM sige_sam_v3.cursosabiertos as a inner join
                     combinaciondias as b on a.idCombinacionDias = b.idCombinacionDias inner join
                     bloqueshorario as c on a.idBloquesHorario = c.idBloquesHorario
                where a.idCursoAbierto = @IdCursoAbierto";

            await using var selectCommand = new MySqlCommand(selectSql, connection);
            selectCommand.Parameters.AddWithValue("@IdCursoAbierto", idCursoAbierto);

            await using var reader = await selectCommand.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var horaFinTimeSpan = reader.GetTimeSpan("HoraFin");
                var nombreCorto = reader.GetString("nombrecorto");
                var horaInicioTimeSpan = reader.GetTimeSpan("Horainicio");
                var fechaInicio = reader.GetDateTime("FechaInicio");
                var fechaFin = reader.GetDateTime("FechaFin");

                var horaInicio = fechaInicio.Date + horaInicioTimeSpan;
                var horaFin = fechaInicio.Date + horaFinTimeSpan;

                await reader.CloseAsync();

                const string updateSql = @"
                    UPDATE cursosabiertosbbb
                    SET fechaInicio = @FechaInicio,
                        fechaTermino = @FechaTermino,
                        dias = @Dias,
                        horaInicio = @HoraInicio,
                        horaTermino = @HoraTermino
                    WHERE idCursoAbierto = @IdCursoAbierto";

                await using var updateCommand = new MySqlCommand(updateSql, connection);
                updateCommand.Parameters.AddWithValue("@FechaInicio", fechaInicio.Date);
                updateCommand.Parameters.AddWithValue("@FechaTermino", fechaFin.Date);
                updateCommand.Parameters.AddWithValue("@Dias", nombreCorto);
                updateCommand.Parameters.AddWithValue("@HoraInicio", horaInicio);
                updateCommand.Parameters.AddWithValue("@HoraTermino", horaFin);
                updateCommand.Parameters.AddWithValue("@IdCursoAbierto", idCursoAbierto);

                var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar el horario del curso desde la fuente externa: {IdCursoAbierto}", idCursoAbierto);
            throw;
        }
    }
}
