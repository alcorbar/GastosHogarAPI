using System.ComponentModel.DataAnnotations;

namespace GastosHogarAPI.Models.DTOs
{
    public class CrearGrupoRequest
    {
        public int UsuarioId { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Descripcion { get; set; }
    }

    public class UnirseGrupoRequest
    {
        public int UsuarioId { get; set; }

        [Required]
        [StringLength(6)]
        public string CodigoInvitacion { get; set; } = string.Empty;
    }

    public class UnirseGrupoUsuarioRequest
    {
        public int UsuarioId { get; set; }
        public int UsuarioDestinoId { get; set; }
    }

    public class BuscarUsuarioRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class GrupoResponse
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int GrupoId { get; set; }
        public string? NombreGrupo { get; set; }
        public string? CodigoInvitacion { get; set; }
        public List<MiembroGrupo> Miembros { get; set; } = new();
    }

    public class UsuarioBusquedaResponse
    {
        public bool Encontrado { get; set; }
        public bool TieneGrupo { get; set; }
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string? FotoUrl { get; set; }
        public string ColorPersonalizado { get; set; } = string.Empty;
        public int? GrupoId { get; set; }
        public string? NombreGrupo { get; set; }
        public string? CodigoGrupo { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class MiembroGrupo
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public string ColorPersonalizado { get; set; } = string.Empty;
        public bool EsCreador { get; set; }
        public DateTime FechaIngreso { get; set; }
        public DateTime UltimoAcceso { get; set; }
        public decimal TotalGastado { get; set; }
        public int GastosCreados { get; set; }
    }
}