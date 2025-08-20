using GastosHogarAPI.Models;

namespace GastosHogarAPI.Services.Interfaces
{
    public interface ILiquidacionService
    {
        // ✅ Método principal - confirmar gastos por usuario
        Task<bool> ConfirmarGastosAsync(int usuarioId, int mes, int año);

        // ✅ Métodos por grupo (para administración)
        Task<EstadoMensual?> ObtenerEstadoMensualAsync(int grupoId, int mes, int año);
        Task<bool> MarcarPagoRealizadoAsync(int grupoId, int mes, int año, string metodoPago);
        Task<bool> ConfirmarPagoRecibidoAsync(int grupoId, int mes, int año);

        // ✅ Métodos de conveniencia (deducen grupo del usuario)
        Task<EstadoMensual?> ObtenerEstadoMensualPorUsuarioAsync(int usuarioId, int mes, int año);
        Task<bool> PuedeConfirmarAsync(int usuarioId, int mes, int año);

        // ✅ Métodos de información
        Task<List<int>> ObtenerUsuariosPendientesConfirmacionAsync(int grupoId, int mes, int año);
        Task<int> ContarConfirmacionesAsync(int grupoId, int mes, int año);
    }
}