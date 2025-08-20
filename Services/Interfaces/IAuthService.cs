using GastosHogarAPI.Models.DTOs;
using LoginRequest = GastosHogarAPI.Models.DTOs.LoginRequest; // Específico para evitar conflictos

namespace GastosHogarAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request);
        Task<LoginResponse> AutoLoginAsync(AutoLoginRequest request);
        Task<UsuarioBusquedaResponse> BuscarUsuarioPorEmailAsync(string email);
        Task<bool> CambiarPinAsync(int usuarioId, string nuevoPin);
    }
}