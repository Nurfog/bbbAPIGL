using System;

namespace bbbAPIGL.Models
{
    public class CursoAbiertoSesion
    {
        public int IdCursoAbierto { get; set; }
        public int SesionNumero { get; set; }
        public DateOnly? Fecha { get; set; }
        public string? TipoSesion { get; set; }
        public bool Activo { get; set; }
        public DateOnly? FechaNuevaSesion { get; set; }
        public string? IdCalendario { get; set; }
    }
}

