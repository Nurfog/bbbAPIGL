using Microsoft.Extensions.Configuration;
using MySqlConnector;
using System.Collections.Generic;
using System.Threading.Tasks;
using bbbAPIGL.Models;

namespace bbbAPIGL.Repositories;

public class MySqlCursoRepository : ICursoRepository
{
    private readonly string _connectionString;

    public MySqlCursoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("MySqlDb")!;
    }
    
    public async Task<List<string>> ObtenerCorreosPorCursoAsync(string idCurso)
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

    public async Task<List<(string IdAlumno, string Email)>> ObtenerAlumnosConCorreosPorCursoAsync(int idCursoAbierto)
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

    public async Task<CursoAbiertoSala?> ObtenerDatosSalaPorCursoAsync(int idCursoAbierto)
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

    public async Task<bool> DesasociarSalaDeCursosAsync(Guid roomId)
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

    public async Task ActualizarIdCalendarioCursoAsync(int idCursoAbierto, string idCalendario)
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

    public async Task GuardarInvitacionAsync(CursoAbiertoInvitacion invitacion)
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

    public async Task<CursoAbiertoInvitacion?> ObtenerInvitacionPorCursoAlumnoAsync(int idCursoAbierto, string idAlumno)
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

    public async Task<string?> ObtenerCorreoPorAlumnoAsync(string idAlumno, int idCursoAbierto)
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
}
