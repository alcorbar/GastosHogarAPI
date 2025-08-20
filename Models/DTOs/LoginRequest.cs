using System.ComponentModel.DataAnnotations;

namespace GastosHogarAPI.Models.DTOs
{
    // Mantener compatibilidad con login PIN
    public class LoginRequest
    {
        public string Usuario { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }

    // NUEVO: Login con Google
    public class GoogleLoginRequest
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;

        [Required]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        public string DeviceName { get; set; } = string.Empty;

        public string? TipoDispositivo { get; set; }
        public string? VersionSO { get; set; }
    }

    // NUEVO: Auto login para dispositivos conocidos
    public class AutoLoginRequest
    {
        [Required]
        public string DeviceId { get; set; } = string.Empty;
    }

    // ACTUALIZAR LoginResponse (más completa)
    public class LoginResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Información adicional del usuario
        public string? Alias { get; set; }
        public string? FotoUrl { get; set; }
        public string ColorPersonalizado { get; set; } = string.Empty;

        // NUEVO: Token JWT
        public string? Token { get; set; }
        public DateTime? TokenExpiration { get; set; }

        // Información del grupo
        public bool RequiereGrupo { get; set; } = false;
        public GrupoInfo? Grupo { get; set; }

        public bool EsNuevoUsuario { get; set; } = false;
    }

    // NUEVO: Información básica del grupo
    public class GrupoInfo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int CantidadMiembros { get; set; }
        public string? CodigoInvitacion { get; set; }
        public bool EsCreador { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}