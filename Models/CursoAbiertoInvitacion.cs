using System;

namespace bbbAPIGL.Models
{
    public class CursoAbiertoInvitacion
    {
        public int Id { get; set; } // Primary Key
        public int IdCursoAbiertoBbb { get; set; }
        public string IdAlumno { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? IdCalendario { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
