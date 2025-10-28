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

    public async Task<CursoAbiertoSala?> ObtenerDatosSalaPorCursoAsync(int idCursoAbierto)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        
        string sql = @"
            SELECT 
                idCursoAbierto, roomId, urlSala, claveModerador, claveEspectador, 
                meetingId, friendlyId, recordId, nombreSala,
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
                FechaInicio = reader.IsDBNull(reader.GetOrdinal("fechaInicio")) ? default : reader.GetDateTime("fechaInicio"),
                FechaTermino = reader.IsDBNull(reader.GetOrdinal("fechaTermino")) ? default : reader.GetDateTime("fechaTermino"),
                Dias = reader.IsDBNull(reader.GetOrdinal("dias")) ? null : reader.GetString("dias"),
                HoraInicio = reader.IsDBNull(reader.GetOrdinal("horaInicio")) ? default : reader.GetTimeSpan("horaInicio"),
                HoraTermino = reader.IsDBNull(reader.GetOrdinal("horaTermino")) ? default : reader.GetTimeSpan("horaTermino")
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
                nombreSala = NULL
            WHERE roomId = @RoomId";

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@RoomId", roomId.ToString());

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
}
