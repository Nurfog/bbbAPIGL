using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace bbbAPIGL.Controllers;

[ApiController]
[Route("test")]
public class TestController : ControllerBase
{
    private readonly IConfiguration _config;

    public TestController(IConfiguration config)
    {
        _config = config;
    }

    [HttpGet("mysql-empresa")]
    public async Task<IActionResult> TestMySqlEmpresa()
    {
        var connString = _config.GetConnectionString("MySqlDbEmpresa");
        try
        {
            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();
            
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM cursosabiertosbbb";
            var result = await cmd.ExecuteScalarAsync();
            
            return Ok(new { 
                success = true, 
                message = "Conexión exitosa a sige_sam_empresa",
                count = result 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                success = false, 
                error = ex.Message,
                connectionString = connString?.Replace(connString.Split('=')[1]?.Split(';')[0], "***")
            });
        }
    }

    [HttpGet("mysql-central")]
    public async Task<IActionResult> TestMySqlCentral()
    {
        var connString = _config.GetConnectionString("MySqlDb");
        try
        {
            await using var conn = new MySqlConnection(connString);
            await conn.OpenAsync();
            
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM cursosabiertosbbb";
            var result = await cmd.ExecuteScalarAsync();
            
            return Ok(new { 
                success = true, 
                message = "Conexión exitosa a sige_sam_v3",
                count = result 
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { 
                success = false, 
                error = ex.Message 
            });
        }
    }
}
