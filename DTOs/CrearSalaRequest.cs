using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace bbbAPIGL.DTOs;

public class CrearSalaRequest
{
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string EmailCreador { get; set; } = string.Empty;

    public List<string>? CorreosParticipantes { get; set; }
}