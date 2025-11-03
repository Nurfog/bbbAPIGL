/// <summary>
/// DTO para la respuesta del envío de invitación a un curso.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 07-10-2025
/// </summary>
namespace bbbAPIGL.DTOs;

public class EnviarInvitacionCursoResponse
{
    /// <summary>
    /// Obtiene o establece un mensaje de respuesta.
    /// </summary>
    public string? Mensaje { get; set; }
    /// <summary>
    /// Obtiene o establece el número de correos electrónicos enviados.
    /// </summary>
    public int CorreosEnviados { get; set; }
}