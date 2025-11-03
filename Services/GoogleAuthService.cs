using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

/// <summary>
/// Servicio para la autenticación con Google APIs.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 19-10-2025
/// </summary>
namespace bbbAPIGL.Services;

public static class GoogleAuthService
{
    /// <summary>
    /// Obtiene las credenciales de Google para la autenticación.
    /// </summary>
    /// <param name="configuration">La configuración de la aplicación.</param>
    /// <param name="logger">El logger para registrar información.</param>
    /// <param name="scope">El ámbito de acceso requerido para la API de Google.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es un objeto GoogleCredential.</returns>
    public static async Task<GoogleCredential> GetGoogleCredentialAsync(IConfiguration configuration, ILogger logger, string scope)
    {
        var settings = configuration.GetSection("GoogleCalendarSettings");
        var credentialsFilePath = settings["CredentialsFile"];
        var userToImpersonate = settings["UserToImpersonate"];

        if (string.IsNullOrWhiteSpace(credentialsFilePath) || string.IsNullOrWhiteSpace(userToImpersonate))
        {
            logger.LogError("El archivo de credenciales o el usuario a suplantar no están configurados.");
            throw new System.ArgumentException("La configuración de credenciales de Google es inválida.");
        }

        await using var stream = new FileStream(credentialsFilePath, FileMode.Open, FileAccess.Read);
        return GoogleCredential.FromStream(stream)
            .CreateScoped(scope)
            .CreateWithUser(userToImpersonate);
    }
}
