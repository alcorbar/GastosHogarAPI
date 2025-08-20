using System.ComponentModel.DataAnnotations;

namespace GastosHogarAPI.Models
{
    public class Grupo
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(6)]
        public string CodigoInvitacion { get; set; } = string.Empty;

        public int CreadorId { get; set; }

        [StringLength(500)]
        public string? Descripcion { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public bool Activo { get; set; } = true;

        public decimal TotalGastado { get; set; } = 0;
        public int TotalLiquidaciones { get; set; } = 0;
        public DateTime? UltimaActividad { get; set; }

        // ✅ Navigation properties básicas - SIN las problemáticas
        public Usuario? Creador { get; set; }
        public List<Usuario> Miembros { get; set; } = new();
        public List<Gasto> Gastos { get; set; } = new();

        // ❌ ESTAS ESTÁN COMENTADAS para evitar conflictos de EF
        // public virtual ICollection<PlanPago> PlanesPago { get; set; } = new List<PlanPago>();
        // public virtual ICollection<EstadoMensual> EstadosMensuales { get; set; } = new List<EstadoMensual>();
    }
}