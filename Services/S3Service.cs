using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace bbbAPIGL.Services;

/// <summary>
/// Servicio para interactuar con Amazon S3, permitiendo la generación de URLs pre-firmadas.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 22-10-2025
/// </summary>
public class S3Service : IS3Service
{
    private readonly string _bucketName;
    private readonly IAmazonS3 _s3Client;

    /// <summary>
    /// Inicializa una nueva instancia del servicio S3.
    /// </summary>
    /// <param name="configuration">La configuración de la aplicación para obtener los ajustes de S3.</param>
    public S3Service(IConfiguration configuration)
    {
        var settings = configuration.GetSection("S3Settings");
        _bucketName = settings["BucketName"]!;
        var region = RegionEndpoint.GetBySystemName(settings["Region"]!);
        _s3Client = new AmazonS3Client(region);
    }

    /// <summary>
    /// Genera una URL pre-firmada para un objeto de S3, válida por una hora.
    /// </summary>
    /// <param name="key">La clave del objeto en el bucket de S3.</param>
    /// <returns>Una URL pre-firmada que permite el acceso temporal al objeto.</returns>
    public Task<string> GetPresignedUrlAsync(string key)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddHours(1) // La URL será válida por 1 hora
        };

        string url = _s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }
}
