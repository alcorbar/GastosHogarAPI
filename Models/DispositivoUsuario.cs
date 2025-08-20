using System.ComponentModel.DataAnnotations;

namespace GastosHogarAPI.Models
{
    public class DispositivoUsuario
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }

        [Required]
        [StringLength(100)]
        public string DeviceId { get; set; } = string.Empty;

        [StringLength(100)]
        public string NombreDispositivo { get; set; } = string.Empty;

        [StringLength(50)]
        public string? TipoDispositivo { get; set; }

        [StringLength(20)]
        public string? VersionSO { get; set; }

        public DateTime FechaVinculacion { get; set; } = DateTime.Now;
        public DateTime UltimoAcceso { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;

        // Navigation
        public Usuario? Usuario { get; set; }
    }
}