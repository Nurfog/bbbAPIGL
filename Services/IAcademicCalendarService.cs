using System;

namespace bbbAPIGL.Services
{
    public interface IAcademicCalendarService
    {
        /// <summary>
        /// Verifica si una fecha dada es un día no hábil (feriado o receso académico).
        /// </summary>
        /// <param name="date">La fecha a verificar.</param>
        /// <returns>Verdadero si la fecha es un día no hábil, falso en caso contrario.</returns>
        bool IsNonClassDay(DateOnly date);
    }
}
