using System;

/// <summary>
/// Representa una invitación a un curso abierto.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 10-10-2025
/// </summary>
namespace bbbAPIGL.Models
{
    public class CursoAbiertoInvitacion
    {
        /// <summary>
        /// Obtiene o establece el ID de la invitación (clave primaria).
        /// </summary>
        public int Id { get; set; } // Primary Key
        /// <summary>
        /// Obtiene o establece el ID del curso abierto de BBB.
        /// </summary>
        public int IdCursoAbiertoBbb { get; set; }
        /// <summary>
        /// Obtiene o establece el ID del alumno invitado.
        /// </summary>
        public string IdAlumno { get; set; } = string.Empty;
        /// <summary>
        /// Obtiene o establece la URL de la invitación.
        /// </summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>
        /// Obtiene o establece el ID del calendario asociado a la invitación.
        /// </summary>
        public string? IdCalendario { get; set; }
        /// <summary>
        /// Obtiene o establece la fecha de creación de la invitación.
        /// </summary>
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}
