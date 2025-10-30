namespace bbbAPIGL.Models;

public class Sala
{
    public required string Nombre { get; set; }
    public required string MeetingId { get; set; }
    public required string FriendlyId { get; set; }
    public required string ClaveModerador { get; set; }
    public required string ClaveEspectador { get; set; }
    public string? IdCalendario { get; set; }
}
