using GastosHogarAPI.Models;
using GastosHogarAPI.Models.DTOs;

namespace GastosHogarAPI.Services.Interfaces
{
    public interface IPlanPagoService
    {
        Task<int> CrearPlanPagoAsync(CrearPlanPagoRequest request);
        Task<List<PlanPagoResponse>> ObtenerPlanesPorGrupoAsync(int grupoId);
        Task<List<PlanPagoResponse>> ObtenerPlanesPorUsuarioAsync(int usuarioId);
        Task<PlanPagoDetalleResponse?> ObtenerDetallePlanAsync(int planId);
        Task<bool> PagarCuotaAsync(PagarCuotaRequest request);
        Task<bool> ConfirmarCuotaAsync(int cuotaId, int usuarioId);
        Task<List<CuotaPendienteResponse>> ObtenerCuotasPendientesAsync(int usuarioId);
        Task<bool> CompletarPlanAsync(int planId);
        Task<bool> CancelarPlanAsync(int planId);
    }
}