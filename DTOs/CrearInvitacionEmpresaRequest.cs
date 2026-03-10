using System;
using System.ComponentModel.DataAnnotations;

namespace bbbAPIGL.DTOs
{
    /// <summary>
    /// DTO para crear una invitación/registro de sesión en el módulo EMP.
    /// Se guarda en la tabla `sesionescursos` y está ligada a un curso abierto.
    /// </summary>
    public class CrearInvitacionEmpresaRequest
    {
        /// <summary>
        /// Identificador del curso abierto en la base de datos de empresa.
        /// </summary>
        [Required]
        public int IdCursoAbierto { get; set; }

        /// <summary>
    /// Identificador de la sesión en la tabla sesionescursos.
    /// </summary>
    [Required]
    public int Id { get; set; }
        /// <summary>
        /// Fecha en que se realizará la sesión/invitación.
        /// </summary>
        [Required]
        public DateOnly Fecha { get; set; }
    }
}