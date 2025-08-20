using GastosHogarAPI.Models.DTOs;

namespace GastosHogarAPI.Services.Interfaces
{
    public interface IGrupoService
    {
        Task<GrupoResponse> CrearGrupoAsync(CrearGrupoRequest request);
        Task<GrupoResponse> UnirseGrupoAsync(UnirseGrupoRequest request);
        Task<GrupoResponse> UnirseGrupoUsuarioAsync(UnirseGrupoUsuarioRequest request);
        Task<bool> SalirGrupoAsync(int usuarioId);
        Task<List<MiembroGrupo>> ObtenerMiembrosGrupoAsync(int grupoId);
    }
}