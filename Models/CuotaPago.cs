using System.ComponentModel.DataAnnotations;

namespace GastosHogarAPI.Models
{
    public class CuotaPago
    {
        public int Id { get; set; }
        public int PlanPagoId { get; set; }
        public int NumeroCuota { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal Monto { get; set; }

        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }

        public EstadoCuota Estado { get; set; } = EstadoCuota.Pendiente;

        [StringLength(50)]
        public string? MetodoPago { get; set; }

        public byte[]? ComprobantePago { get; set; }

        [StringLength(500)]
        public string? NotasPago { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaConfirmacion { get; set; }
        public bool Confirmado { get; set; } = false;

        // ✅ SOLO esta navigation property
        public virtual PlanPago? PlanPago { get; set; }
    }

    // ✅ ENUMS bien definidos
    public enum EstadoCuota
    {
        Pendiente,
        Pagada,
        Vencida,
        Confirmada,
        Cancelada
    }

    public enum EstadoPago
    {
        Pendiente,
        Pagado,
        Parcial,
        Vencido,
        Confirmado
    }
}