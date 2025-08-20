using System.ComponentModel.DataAnnotations;

namespace GastosHogarAPI.Models.DTOs
{
    // ✅ Request para crear un plan de pago
    public class CrearPlanPagoRequest
    {
        public int GrupoId { get; set; }
        public int DeudorId { get; set; }
        public int AcreedorId { get; set; }

        [Required]
        [Range(0.01, 999999.99)]
        public decimal MontoTotal { get; set; }

        [Range(2, 12)]
        public int NumeroCuotas { get; set; } = 6;

        [Range(1, 365)]
        public int DiasFrecuencia { get; set; } = 30; // Mensual por defecto

        [StringLength(500)]
        public string? Descripcion { get; set; }

        [StringLength(200)]
        public string? Motivo { get; set; }

        public DateTime FechaPrimeraCuota { get; set; } = DateTime.Now.AddDays(30);

        public int? EstadoMensualId { get; set; } // Para vincular con liquidación mensual
    }

    // ✅ Response con información básica del plan
    public class PlanPagoResponse
    {
        public int Id { get; set; }
        public string DeudorNombre { get; set; } = string.Empty;
        public string AcreedorNombre { get; set; } = string.Empty;
        public decimal MontoTotal { get; set; }
        public decimal MontoCuota { get; set; }
        public int NumeroCuotas { get; set; }
        public int CuotasPagadas { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal MontoRestante { get; set; }
        public string? Descripcion { get; set; }
        public DateTime FechaPrimeraCuota { get; set; }
        public DateTime? FechaCompletado { get; set; }
        public bool Completado { get; set; }
        public bool EsSoyDeudor { get; set; } // Para identificar perspectiva del usuario
        public DateTime? ProximaCuotaFecha { get; set; }
        public decimal PorcentajeCompletado { get; set; }

        // Campos calculados para UI
        public string EstadoTexto => Completado ? "✅ Completado" :
                                   CuotasPagadas == 0 ? "⏳ Pendiente" :
                                   "🔄 En progreso";

        public string ProgresoTexto => $"{CuotasPagadas}/{NumeroCuotas} cuotas";

        public string MontoTexto => EsSoyDeudor ?
            $"Debo €{MontoRestante:F2} de €{MontoTotal:F2}" :
            $"Me deben €{MontoRestante:F2} de €{MontoTotal:F2}";
    }

    // ✅ Response detallada del plan con todas las cuotas
    public class PlanPagoDetalleResponse
    {
        public int Id { get; set; }
        public string GrupoNombre { get; set; } = string.Empty;

        public int DeudorId { get; set; }
        public string DeudorNombre { get; set; } = string.Empty;
        public string DeudorColor { get; set; } = string.Empty;

        public int AcreedorId { get; set; }
        public string AcreedorNombre { get; set; } = string.Empty;
        public string AcreedorColor { get; set; } = string.Empty;

        public decimal MontoTotal { get; set; }
        public decimal MontoCuota { get; set; }
        public int NumeroCuotas { get; set; }
        public int DiasFrecuencia { get; set; }

        public int CuotasPagadas { get; set; }
        public decimal MontoPagado { get; set; }
        public decimal MontoRestante { get; set; }

        public string? Descripcion { get; set; }
        public string? Motivo { get; set; }

        public DateTime FechaCreacion { get; set; }
        public DateTime FechaPrimeraCuota { get; set; }
        public DateTime? FechaCompletado { get; set; }

        public bool Activo { get; set; }
        public bool Completado { get; set; }
        public int? EstadoMensualId { get; set; }

        public decimal PorcentajeCompletado { get; set; }

        public List<CuotaResponse> Cuotas { get; set; } = new();

        // Campos calculados
        public string FrecuenciaTexto => DiasFrecuencia switch
        {
            7 => "Semanal",
            14 => "Quincenal",
            30 => "Mensual",
            _ => $"Cada {DiasFrecuencia} días"
        };
    }

    // ✅ Response para una cuota individual
    public class CuotaResponse
    {
        public int Id { get; set; }
        public int NumeroCuota { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public DateTime? FechaPago { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? MetodoPago { get; set; }
        public string? NotasPago { get; set; }
        public bool Confirmado { get; set; }
        public int? DiasRestantes { get; set; }
        public bool EstaVencida { get; set; }

        // Campos calculados para UI
        public string EstadoEmoji => Estado switch
        {
            "Pendiente" => "⏳",
            "Pagada" => "💰",
            "Confirmada" => "✅",
            "Vencida" => "⚠️",
            "Cancelada" => "❌",
            _ => "❓"
        };

        public string EstadoTexto => Estado switch
        {
            "Pendiente" => EstaVencida ? "Vencida" : "Pendiente",
            "Pagada" => Confirmado ? "Confirmada" : "Pendiente confirmación",
            "Confirmada" => "Confirmada",
            "Vencida" => "Vencida",
            "Cancelada" => "Cancelada",
            _ => Estado
        };

        public string FechaTexto => FechaPago?.ToString("dd/MM/yyyy") ??
                                   FechaVencimiento.ToString("dd/MM/yyyy");
    }

    // ✅ Request para pagar una cuota
    public class PagarCuotaRequest
    {
        public int CuotaId { get; set; }

        [Required]
        [StringLength(50)]
        public string MetodoPago { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Notas { get; set; }

        public byte[]? ComprobantePago { get; set; }
    }

    // ✅ Response para cuotas pendientes del usuario
    public class CuotaPendienteResponse
    {
        public int CuotaId { get; set; }
        public int PlanId { get; set; }
        public int NumeroCuota { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string DeudorNombre { get; set; } = string.Empty;
        public string AcreedorNombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool EsSoyDeudor { get; set; }
        public bool RequiereAccion { get; set; }
        public int DiasRestantes { get; set; }
        public bool EstaVencida { get; set; }

        // Campos calculados para UI
        public string AccionTexto => EsSoyDeudor ?
            (Estado == "Pendiente" || Estado == "Vencida" ? "💰 Pagar" : "⏳ Esperando confirmación") :
            (Estado == "Pagada" ? "✅ Confirmar" : "👀 Ver");

        public string UrgenciaTexto => EstaVencida ? "🚨 Vencida" :
                                      DiasRestantes <= 3 ? "⚠️ Próxima" :
                                      "📅 Planificada";

        public string RelacionTexto => EsSoyDeudor ?
            $"Pagar a {AcreedorNombre}" :
            $"{DeudorNombre} me debe";
    }

    // ✅ Response de resumen de planes por usuario
    public class ResumenPlanesUsuarioResponse
    {
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;

        public int PlanesComoDeudor { get; set; }
        public int PlanesComoAcreedor { get; set; }
        public int PlanesActivos { get; set; }
        public int PlanesCompletados { get; set; }

        public decimal TotalDebo { get; set; }
        public decimal TotalMeDeben { get; set; }
        public decimal BalanceNeto { get; set; }

        public int CuotasPendientesPagar { get; set; }
        public int CuotasPendientesConfirmar { get; set; }

        public List<CuotaPendienteResponse> ProximasCuotas { get; set; } = new();

        // Campos calculados
        public string BalanceTexto => BalanceNeto >= 0 ?
            $"Me deben €{BalanceNeto:F2}" :
            $"Debo €{Math.Abs(BalanceNeto):F2}";
    }
}