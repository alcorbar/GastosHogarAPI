namespace GastosHogarAPI.Models.DTOs
{
    public class ResumenMensual
    {
        public int Mes { get; set; }
        public int Año { get; set; }
        public int GrupoId { get; set; }

        // ✅ Sistema flexible para cualquier número de usuarios
        public Dictionary<int, decimal> GastosPorUsuario { get; set; } = new();
        public Dictionary<int, decimal> DetallesPorUsuario { get; set; } = new();
        public Dictionary<int, decimal> GastosCompartidosPorUsuario { get; set; } = new();

        // ✅ Información detallada de usuarios del grupo
        public Dictionary<int, UsuarioResumen> InformacionUsuarios { get; set; } = new();

        // Totales generales
        public decimal TotalGastos { get; set; }
        public decimal TotalDetalles { get; set; }
        public decimal TotalCompartido { get; set; }
        public decimal CuotaPorPersona { get; set; }

        // Resultado de liquidación
        public int? DeudorId { get; set; }
        public int? AcreedorId { get; set; }
        public decimal MontoDeuda { get; set; }
        public string? NombreDeudor { get; set; }
        public string? NombreAcreedor { get; set; }

        // Estados
        public Dictionary<int, bool> ConfirmacionesUsuarios { get; set; } = new();
        public bool TodosConfirmaron { get; set; }
        public bool LiquidacionCalculada { get; set; }
        public string EstadoPago { get; set; } = "pendiente";

        // Plan de pago asociado
        public int? PlanPagoId { get; set; }

        // Explicación paso a paso
        public string ExplicacionDetallada { get; set; } = string.Empty;
        public List<string> PasosExplicacion { get; set; } = new();
    }

    public class UsuarioResumen
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Alias { get; set; }
        public string ColorPersonalizado { get; set; } = string.Empty;
        public string? FotoUrl { get; set; }
        public bool Confirmado { get; set; }
        public decimal TotalGastado { get; set; }
        public decimal DetallesGastados { get; set; }
        public decimal CompartidoGastado { get; set; }
    }
}