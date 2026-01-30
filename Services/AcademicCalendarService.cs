using System;
using System.Collections.Generic;
using System.Linq;

namespace bbbAPIGL.Services
{
    public class AcademicCalendarService : IAcademicCalendarService
    {
        private readonly HashSet<DateOnly> _nonClassDays = new HashSet<DateOnly>();

        public AcademicCalendarService()
        {
            // Aquí se cargarían los días no hábiles desde una base de datos, un archivo de configuración o un servicio externo.
            // Por ahora, usaremos una lista harcodeada a modo de ejemplo.
            
            // Feriados 2025 (Ejemplos)
            _nonClassDays.Add(new DateOnly(2025, 1, 1));   // Año Nuevo
            _nonClassDays.Add(new DateOnly(2025, 4, 18));  // Viernes Santo
            _nonClassDays.Add(new DateOnly(2025, 4, 19));  // Sábado Santo
            _nonClassDays.Add(new DateOnly(2025, 5, 1));   // Día del Trabajo
            _nonClassDays.Add(new DateOnly(2025, 5, 21));  // Día de las Glorias Navales
            _nonClassDays.Add(new DateOnly(2025, 6, 29));  // San Pedro y San Pablo
            _nonClassDays.Add(new DateOnly(2025, 7, 16));  // Día de la Virgen del Carmen
            _nonClassDays.Add(new DateOnly(2025, 8, 15));  // Asunción de la Virgen
            _nonClassDays.Add(new DateOnly(2025, 9, 18));  // Fiestas Patrias
            _nonClassDays.Add(new DateOnly(2025, 9, 19));  // Fiestas Patrias
            _nonClassDays.Add(new DateOnly(2025, 10, 12)); // Encuentro de Dos Mundos
            _nonClassDays.Add(new DateOnly(2025, 10, 31)); // Día de las Iglesias Evangélicas y Protestantes
            _nonClassDays.Add(new DateOnly(2025, 11, 1));  // Día de Todos los Santos
            _nonClassDays.Add(new DateOnly(2025, 12, 8));  // Inmaculada Concepción
            _nonClassDays.Add(new DateOnly(2025, 12, 25)); // Navidad

            // Receso académico (Ejemplo)
            // Supongamos un receso de invierno del 14 al 25 de julio.
            for (var date = new DateOnly(2025, 7, 14); date <= new DateOnly(2025, 7, 25); date = date.AddDays(1))
            {
                _nonClassDays.Add(date);
            }
        }

        /// <summary>
        /// Verifica si una fecha dada es un día no hábil (feriado o receso académico).
        /// </summary>
        /// <param name="date">La fecha a verificar.</param>
        /// <returns>Verdadero si la fecha es un día no hábil, falso en caso contrario.</returns>
        public bool IsNonClassDay(DateOnly date)
        {
            return _nonClassDays.Contains(date);
        }
    }
}
