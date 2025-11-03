using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO para la solicitud de envío de invitación a un curso.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 06-10-2025
/// </summary>
namespace bbbAPIGL.DTOs;

public class EnviarInvitacionCursoRequest
{
    /// <summary>
    /// Obtiene o establece el ID del curso abierto. Es un campo requerido.
    /// </summary>
    [Required]
    public int IdCursoAbierto { get; set; } 
}