using System;

/// <summary>
/// Representa la información de una grabación.
/// Autor: Juan Enrique Allende Cifuentes
/// Fecha de Creación: 12-10-2025
/// </summary>
namespace bbbAPIGL.Models;

public class RecordingInfo
{
    /// <summary>
    /// Obtiene o establece el ID de la grabación.
    /// </summary>
    public string RecordId { get; set; } = string.Empty;
    /// <summary>
    /// Obtiene o establece la fecha de creación de la grabación.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}