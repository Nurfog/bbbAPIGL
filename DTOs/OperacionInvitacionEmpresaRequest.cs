using System;
using System.ComponentModel.DataAnnotations;

namespace bbbAPIGL.DTOs
{
    /// <summary>
    /// Representa una única operación para gestionar invitaciones en el módulo EMP.
    /// "Accion" puede ser "crear", "editar" o "eliminar".
    /// </summary>
    public class OperacionInvitacionEmpresaRequest
    {
        /// <summary>
        /// Tipo de operación: crear, editar o eliminar.
        /// </summary>
        [Required]
        public string Accion { get; set; } = string.Empty;

        /// <summary>
    /// Identificador de la sesión en sesionescursos.
    /// </summary>
    [Required]
    public int Id { get; set; }
        /// <summary>
        /// Fecha de la sesión (para creación).
        /// </summary>
        public DateOnly? Fecha { get; set; }

        /// <summary>
        /// Fecha nueva de la sesión (para edición).
        /// </summary>
        public DateOnly? FechaNueva { get; set; }
    }
}