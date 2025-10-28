using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace bbbAPIGL.Services;

public class S3Service : IS3Service
{
    private readonly string _bucketName;
    private readonly IAmazonS3 _s3Client;

    public S3Service(IConfiguration configuration)
    {
        var settings = configuration.GetSection("S3Settings");
        _bucketName = settings["BucketName"]!;
        var region = RegionEndpoint.GetBySystemName(settings["Region"]!);
        _s3Client = new AmazonS3Client(region);
    }

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