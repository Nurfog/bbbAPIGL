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

    /// <summary>
    /// Obtiene la lista de correos de los alumnos para un curso específico.
    /// </summary>
    public async Task<List<string>> ObtenerCorreosPorCursoAsync(string idCurso)
    {
        var emails = new List<string>();
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

    
        string sql = "SELECT alu.Email " +
                        "FROM sige_sam_v3.detallecontrato as detcon inner join " +
                        "alumnos as alu on detcon.idAlumno = alu.idAlumno " +
                        "where detcon.Activo = 1 and " +
                        "idCursoAbierto = "+ idCurso;
        
        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.AddWithValue("@idCursoAbierto", idCurso);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            // Asume que la columna de email es la primera (índice 0)
            emails.Add(reader.GetString(0));
        }

        return emails;
    }

    /// <summary>
    /// Obtiene los detalles de la sala de BBB desde la tabla de cursos abiertos.
    /// </summary>
    public async Task<CursoAbiertoSala?> ObtenerDatosSalaPorCursoAsync(int idCursoAbierto)
    {
        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();
        
        string sql = @"
            SELECT idCursoAbierto, roomId, urlSala, claveModerador, claveEspectador, meetingId, friendlyId, recordId, nombreSala 
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
                RoomId = reader.GetString("roomId"),
                UrlSala = reader.GetString("urlSala"),
                ClaveModerador = reader.GetString("claveModerador"),
                ClaveEspectador = reader.GetString("claveEspectador"),
                MeetingId = reader.GetString("meetingId"),
                FriendlyId = reader.GetString("friendlyId"),
                RecordId = reader.GetString("recordId"),
                NombreSala = reader.GetString("nombreSala")
            };
        }

        return null;
    }
}