using System.ComponentModel.DataAnnotations;

namespace bbbAPIGL.DTOs;

public class EnviarInvitacionIndividualRequest
{
    [Required]
    public string IdAlumno { get; set; } = string.Empty;

    [Required]
    public int IdCursoAbierto { get; set; }
}
