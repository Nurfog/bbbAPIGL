using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO para la solicitud de creación de una sala.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 04-10-2025
/// </summary>
namespace bbbAPIGL.DTOs;

public class CrearSalaRequest
{
    /// <summary>
    /// Obtiene o establece el nombre de la sala. Es un campo requerido.
    /// </summary>
    [Required]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Obtiene o establece el correo electrónico del creador de la sala. 
    /// Para el módulo de empresas, si no se proporciona, se usa el configurado en DefaultRoomCreatorEmailEmpresa.
    /// </summary>
    [EmailAddress]
    public string? EmailCreador { get; set; }

    /// <summary>
    /// Obtiene o establece el ID del curso abierto al que se asociará la sala.
    /// </summary>
    [Required]
    public int IdCursoAbierto { get; set; }
}