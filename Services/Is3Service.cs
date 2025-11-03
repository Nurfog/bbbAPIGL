using System.Threading.Tasks;

/// <summary>
/// Interfaz para el servicio de Amazon S3.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 22-10-2025
/// </summary>
namespace bbbAPIGL.Services;

public interface IS3Service
{
    /// <summary>
    /// Obtiene una URL pre-firmada para acceder a un objeto en S3.
    /// </summary>
    /// <param name="key">La clave del objeto en S3.</param>
    /// <returns>Una tarea que representa la operación asíncrona. El resultado es la URL pre-firmada.</returns>
    Task<string> GetPresignedUrlAsync(string key);
}