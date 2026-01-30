namespace bbbAPIGL.DTOs
{
    public class ReprogramarSesionRequest
    {
        public required int IdCursoAbierto { get; set; }
        public int SesionNumero { get; set; }
        public DateOnly FechaOriginalSesion { get; set; }
        public DateOnly FechaNuevaSesion { get; set; }
    }
}
