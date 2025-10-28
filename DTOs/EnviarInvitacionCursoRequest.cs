using System.ComponentModel.DataAnnotations;

namespace bbbAPIGL.DTOs;

public class EnviarInvitacionCursoRequest
{
    [Required]
    public int IdCursoAbierto { get; set; } 
}