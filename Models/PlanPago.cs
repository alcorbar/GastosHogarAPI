using System.ComponentModel.DataAnnotations;

namespace GastosHogarAPI.Models
{
    public class PlanPago
    {
        public int Id { get; set; }
        public int GrupoId { get; set; }
        public int DeudorId { get; set; }
        public int AcreedorId { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal MontoTotal { get; set; }

        [Range(2, 12)]
        public int NumeroCuotas { get; set; }

        [Range(1, 365)]
        public int DiasFrecuencia { get; set; }

        public decimal MontoCuota { get; set; }

        public int? EstadoMensualId { get; set; } // ✅ opcional

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [StringLength(200)]
        public string? Motivo { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime FechaPrimeraCuota { get; set; }
        public DateTime? FechaCompletado { get; set; }

        public bool Activo { get; set; } = true;
        public bool Completado { get; set; } = false;

        public int CuotasPagadas { get; set; } = 0;
        public decimal MontoPagado { get; set; } = 0;
        public decimal MontoRestante { get; set; }

        // ✅ Solo cuotas
        public virtual ICollection<CuotaPago> Cuotas { get; set; } = new List<CuotaPago>();

        // ❌ Evitamos la navegación a EstadoMensual para no crear doble FK
        // public virtual EstadoMensual? EstadoMensual { get; set; }
    }
}
