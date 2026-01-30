using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO para la solicitud de envío de invitación individual.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 08-10-2025
/// </summary>
namespace bbbAPIGL.DTOs;

public class EnviarInvitacionIndividualRequest
{
    /// <summary>
    /// Obtiene o establece el ID del alumno. Es un campo requerido.
    /// </summary>
    [Required]
    public string IdAlumno { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el ID del curso abierto. Es un campo requerido.
    /// </summary>
    [Required]
    public int IdCursoAbierto { get; set; }
}
