using System;
using System.Collections.Generic;

namespace bbbAPIGL.DTOs;

public class ActualizarEventoCalendarioRequest
{
    public int IdCursoAbierto { get; set; }
    public List<string>? CorreosParticipantes { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaTermino { get; set; }
    public string? DiasSemana { get; set; }
    public DateTime? HoraInicio { get; set; }
    public DateTime? HoraTermino { get; set; }
}