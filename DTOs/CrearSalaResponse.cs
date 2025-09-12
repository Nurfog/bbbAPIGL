namespace bbbAPIGL.DTOs;

public class CrearSalaResponse
{
    public Guid RoomId { get; set; }
    public string UrlSala { get; set; } = string.Empty;
    public string ClaveModerador { get; set; } = string.Empty;
    public string ClaveEspectador { get; set; } = string.Empty;
    public string MeetingId { get; set; } = string.Empty;
    public string FriendlyId { get; set; } = string.Empty;
    public string RecordId { get; set; } = string.Empty;
}