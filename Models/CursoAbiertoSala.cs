using System;

namespace bbbAPIGL.Models;

public class CursoAbiertoSala
{
    public int IdCursoAbierto { get; set; }
    public string? RoomId { get; set; }
    public string? UrlSala { get; set; }
    public string? ClaveModerador { get; set; }
    public string? ClaveEspectador { get; set; }
    public string? MeetingId { get; set; }
    public string? FriendlyId { get; set; }
    public string? RecordId { get; set; }
    public string? NombreSala { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaTermino { get; set; }
    public string? Dias { get; set; }
    public DateTime HoraInicio { get; set; }
    public DateTime HoraTermino { get; set; }
}
