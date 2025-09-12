using System;
using System.ComponentModel.DataAnnotations;

namespace bbbAPIGL.DTOs;

public class EliminarSalaRequest
{
    [Required(ErrorMessage = "El RoomId es obligatorio.")]
    public Guid RoomId { get; set; }
}