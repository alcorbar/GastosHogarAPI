using GastosHogarAPI.Models;
using GastosHogarAPI.Models.DTOs;

namespace GastosHogarAPI.Services.Interfaces
{
    public interface IGastosService
    {
        Task<int> CrearGastoAsync(Gasto gasto);
        Task<int> CrearGastoAsync(CrearGastoRequest request);
        Task<List<object>> ObtenerGastosMesAsync(int mes, int año);
        Task<List<GastoResponse>> ObtenerGastosMesGrupoAsync(int grupoId, int mes, int año);
        Task<ResumenMensual> ObtenerResumenMensualAsync(int mes, int año);
        Task<ResumenMensual> ObtenerResumenMensualGrupoAsync(int grupoId, int mes, int año);
    }
}