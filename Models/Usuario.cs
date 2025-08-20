using System.ComponentModel.DataAnnotations;

namespace GastosHogarAPI.Models
{
    public class Usuario
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Alias { get; set; }

        public string Pin { get; set; } = string.Empty;

        public string? GoogleId { get; set; }
        public string? FotoUrl { get; set; }
        public byte[]? FotoPerfil { get; set; }

        [StringLength(7)]
        public string ColorPersonalizado { get; set; } = "#2196F3";

        public int? GrupoId { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime UltimoAcceso { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;

        public decimal TotalGastado { get; set; } = 0;
        public decimal TotalPagado { get; set; } = 0;
        public int GastosCreados { get; set; } = 0;

        // ✅ Navigation properties básicas - SIN las problemáticas
        public Grupo? Grupo { get; set; }
        public List<DispositivoUsuario> Dispositivos { get; set; } = new();
        public List<Gasto> Gastos { get; set; } = new();

        // ❌ ESTAS ESTÁN COMENTADAS para evitar conflictos de EF
        // public virtual ICollection<PlanPago> PlanesComoDeudor { get; set; } = new List<PlanPago>();
        // public virtual ICollection<PlanPago> PlanesComoAcreedor { get; set; } = new List<PlanPago>();
        // public virtual ICollection<EstadoMensual> EstadosComoDeudor { get; set; } = new List<EstadoMensual>();
        // public virtual ICollection<EstadoMensual> EstadosComoAcreedor { get; set; } = new List<EstadoMensual>();
    }
}