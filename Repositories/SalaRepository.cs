using Npgsql;
using bbbAPIGL.Models;

namespace bbbAPIGL.Repositories;

public class SalaRepository(IConfiguration configuration) : ISalaRepository
{
    private readonly string _connectionString = configuration.GetConnectionString("PostgresDb")!;

    public async Task<Guid?> GuardarSalaAsync(Sala sala, string userEmail)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var userQuery = "SELECT id FROM users WHERE email = @email LIMIT 1;";
            Guid? creatorId;
            await using (var userCmd = new NpgsqlCommand(userQuery, conn))
            {
                userCmd.Parameters.AddWithValue("email", userEmail);
                var result = await userCmd.ExecuteScalarAsync();
                if (result == null)
                {
                    throw new InvalidOperationException($"El usuario '{userEmail}' no fue encontrado.");
                }
                creatorId = (Guid)result;
            }

            var roomQuery = @"
                INSERT INTO rooms (name, meeting_id, user_id, friendly_id, calendar_event_id, created_at, updated_at) 
                VALUES (@name, @meeting_id, @user_id, @friendly_id, @calendar_event_id, @created_at, @updated_at)
                RETURNING id;";

            Guid newRoomId;
            await using (var roomCmd = new NpgsqlCommand(roomQuery, conn))
            {
                roomCmd.Parameters.AddWithValue("name", sala.Nombre);
                roomCmd.Parameters.AddWithValue("meeting_id", sala.MeetingId);
                roomCmd.Parameters.AddWithValue("user_id", creatorId.Value);
                roomCmd.Parameters.AddWithValue("friendly_id", sala.FriendlyId);
                roomCmd.Parameters.AddWithValue("calendar_event_id", (object)sala.IdCalendario ?? DBNull.Value);
                roomCmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
                roomCmd.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
                newRoomId = (Guid)(await roomCmd.ExecuteScalarAsync())!;
            }

            await InsertarOpcionesDeSala(conn, newRoomId, sala.ClaveModerador, sala.ClaveEspectador);

            await transaction.CommitAsync();
            return newRoomId;
        }
        catch
        {
            await transaction.RollbackAsync();
            return null;
        }
    }

    private static async Task InsertarOpcionesDeSala(NpgsqlConnection conn, Guid roomId, string moderatorPw, string attendeePw)
    {
        var optionsData = new Dictionary<Guid, string>
        {
            { new Guid("99216802-f366-4919-bc4e-925b04749790"), "false" },
            { new Guid("114d9563-9c30-41f0-b9ac-864f49e51881"), "true" },
            { new Guid("b9f32073-cef4-4450-927b-0262f35114ac"), "false" },
            { new Guid("c825fe4c-b0d9-4382-9007-6b3976ca3ec3"), "false" },
            { new Guid("8e9aef23-838b-4685-ab4a-fe1212f4e23a"), "true" },
            { new Guid("788e5166-e587-4fb7-b973-bdf820268a72"), "MODERATOR_CODE_VIEWER_CODE" },
            { new Guid("c0c811cb-0f8f-46f2-a7ae-7603da3d2a10"), "" },
            { new Guid("c04c2d89-4a10-4811-ac98-5804f94918c1"), moderatorPw },
            { new Guid("18c3d4ea-5098-419b-8e08-27773ae82df1"), attendeePw },
            { new Guid("cec29aa7-7597-4a48-94e3-9a74f595be40"), "false" }
        };

        const string optionsQuery = @"
            INSERT INTO room_meeting_options (id, room_id, meeting_option_id, value, created_at, updated_at)
            VALUES (@id, @room_id, @meeting_option_id, @value, @created_at, @updated_at);";

        foreach (var option in optionsData)
        {
            await using var optionsCmd = new NpgsqlCommand(optionsQuery, conn);
            optionsCmd.Parameters.AddWithValue("id", Guid.NewGuid());
            optionsCmd.Parameters.AddWithValue("room_id", roomId);
            optionsCmd.Parameters.AddWithValue("meeting_option_id", option.Key);
            optionsCmd.Parameters.AddWithValue("value", option.Value);
            optionsCmd.Parameters.AddWithValue("created_at", DateTime.UtcNow);
            optionsCmd.Parameters.AddWithValue("updated_at", DateTime.UtcNow);
            await optionsCmd.ExecuteNonQueryAsync();
        }
    }
    
    public async Task<bool> EliminarSalaAsync(Guid roomId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await using var transaction = await conn.BeginTransactionAsync();

        try
        {
            var optionsQuery = "DELETE FROM room_meeting_options WHERE room_id = @RoomId";
            await using (var optionsCmd = new NpgsqlCommand(optionsQuery, conn))
            {
                optionsCmd.Parameters.AddWithValue("RoomId", roomId);
                await optionsCmd.ExecuteNonQueryAsync();
            }

            var recordingIds = new List<Guid>();
            var getRecordingsQuery = "SELECT id FROM recordings WHERE room_id = @RoomId";
            await using (var getRecordingsCmd = new NpgsqlCommand(getRecordingsQuery, conn))
            {
                getRecordingsCmd.Parameters.AddWithValue("RoomId", roomId);
                await using var reader = await getRecordingsCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    recordingIds.Add(reader.GetGuid(0));
                }
            }

            if (recordingIds.Count > 0)
            {
                var formatsQuery = "DELETE FROM formats WHERE recording_id = ANY(@RecordingIds)";
                await using (var formatsCmd = new NpgsqlCommand(formatsQuery, conn))
                {
                    formatsCmd.Parameters.AddWithValue("RecordingIds", recordingIds);
                    await formatsCmd.ExecuteNonQueryAsync();
                }
            }

            var recordingsQuery = "DELETE FROM recordings WHERE room_id = @RoomId";
            await using (var recordingsCmd = new NpgsqlCommand(recordingsQuery, conn))
            {
                recordingsCmd.Parameters.AddWithValue("RoomId", roomId);
                await recordingsCmd.ExecuteNonQueryAsync();
            }

            var sharedAccessesQuery = "DELETE FROM shared_accesses WHERE room_id = @RoomId";
            await using (var sharedAccessesCmd = new NpgsqlCommand(sharedAccessesQuery, conn))
            {
                sharedAccessesCmd.Parameters.AddWithValue("RoomId", roomId);
                await sharedAccessesCmd.ExecuteNonQueryAsync();
            }

            var roomQuery = "DELETE FROM rooms WHERE id = @RoomId";
            int rowsAffected;
            await using (var roomCmd = new NpgsqlCommand(roomQuery, conn))
            {
                roomCmd.Parameters.AddWithValue("RoomId", roomId);
                rowsAffected = await roomCmd.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"Error al eliminar la sala: {ex.ToString()}");
            throw;
        }
    }
    public async Task<Sala?> ObtenerSalaPorIdAsync(Guid roomId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        string sql = "SELECT name, meeting_id, friendly_id FROM rooms WHERE id = @RoomId";

        await using var command = new NpgsqlCommand(sql, conn);
        command.Parameters.AddWithValue("@RoomId", roomId);

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new Sala
            {
                Nombre = reader.GetString(0),
                MeetingId = reader.GetString(1),
                FriendlyId = reader.GetString(2),
                ClaveModerador = string.Empty,
                ClaveEspectador = string.Empty
            };
        }
        return null;
    }
    public async Task<List<RecordingInfo>> ObtenerTodosLosRecordIdsPorRoomIdAsync(Guid roomId)
    {
        var recordings = new List<RecordingInfo>();
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        
        const string sql = @"
            SELECT record_id, created_at 
            FROM recordings 
            WHERE room_id = @RoomId 
            ORDER BY created_at DESC";
        
        await using var command = new NpgsqlCommand(sql, conn);
        command.Parameters.AddWithValue("RoomId", roomId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            recordings.Add(new RecordingInfo
            {
                RecordId = reader.GetString(0),
                CreatedAt = reader.GetDateTime(1)
            });
        }
        
                return recordings;
        
            }
        
        
        
            public async Task<string?> ObtenerIdCalendarioPorSalaIdAsync(Guid roomId)
        
            {
        
                await using var conn = new NpgsqlConnection(_connectionString);
        
                await conn.OpenAsync();
        
                string sql = "SELECT calendar_event_id FROM rooms WHERE id = @RoomId";
        
        
        
                await using var command = new NpgsqlCommand(sql, conn);
        
                command.Parameters.AddWithValue("@RoomId", roomId);
        
        
        
                var result = await command.ExecuteScalarAsync();
        
                return result is not DBNull ? result?.ToString() : null;
        
            }
        
        }
        
        