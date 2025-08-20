using System.ComponentModel.DataAnnotations;

namespace GastosHogarAPI.Models
{
    public class EstadoMensual
    {
        public int Id { get; set; }

        [Required]
        public int GrupoId { get; set; }

        [Required]
        [Range(1, 12)]
        public int Mes { get; set; }

        [Required]
        [Range(2020, 2100)]
        public int Año { get; set; }

        // ✅ NULLABLE para evitar problemas de cascade
        public int? PlanPagoId { get; set; }

        // Información de liquidación
        public decimal MontoDeuda { get; set; } = 0;
        public int? DeudorId { get; set; }
        public int? AcreedorId { get; set; }

        // Estados y fechas
        [StringLength(20)]
        public string EstadoPago { get; set; } = "pendiente"; // pendiente, pagado, confirmado

        public DateTime? FechaLiquidacion { get; set; }
        public DateTime? FechaPago { get; set; }

        [StringLength(50)]
        public string? MetodoPago { get; set; }

        // Control de confirmaciones
        public bool LiquidacionCalculada { get; set; } = false;
        public bool TodosConfirmaron { get; set; } = false;

        // ✅ Diccionario para confirmaciones multiusuario
        public Dictionary<int, bool> ConfirmacionesUsuarios { get; set; } = new();

        // Navigation properties básicas - SIN incluir las problemáticas
        public Grupo? Grupo { get; set; }
        public Usuario? Deudor { get; set; }
        public Usuario? Acreedor { get; set; }
        public PlanPago? PlanPago { get; set; }
    }
}