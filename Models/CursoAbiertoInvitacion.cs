using System;

namespace bbbAPIGL.Models
{
    public class CursoAbiertoInvitacion
    {
        public int Id { get; set; } // Primary Key
        public int IdCursoAbiertoBbb { get; set; }
        public int IdAlumno { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? IdCalendario { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
