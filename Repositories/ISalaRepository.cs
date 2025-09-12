using System;
using System.Threading.Tasks;
using bbbAPIGL.Models;

namespace bbbAPIGL.Repositories;

public interface ISalaRepository
{
    Task<Guid?> GuardarSalaAsync(Sala sala, string userEmail);
    Task<bool> EliminarSalaAsync(Guid roomId);
    Task<Sala?> ObtenerSalaPorIdAsync(Guid roomId);
}