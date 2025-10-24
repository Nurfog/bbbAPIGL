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
<<<<<<< HEAD
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraTermino { get; set; }
}
=======
}
>>>>>>> bc0cedfcf1b28861c8327da4f8ee316f08aecbf9
