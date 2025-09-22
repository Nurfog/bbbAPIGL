namespace bbbAPIGL.Services;

public interface IS3Service
{
    Task<string> GetPresignedUrlAsync(string key);
}