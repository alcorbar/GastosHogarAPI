using GastosHogarAPI.Data;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GastosHogarAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EstadisticasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IGastosService _gastosService;
        private readonly ILiquidacionService _liquidacionService;
        private readonly IPlanPagoService _planPagoService;
        private readonly ILogger<EstadisticasController> _logger;

        public EstadisticasController(
            AppDbContext context,
            IGastosService gastosService,
            ILiquidacionService liquidacionService,
            IPlanPagoService planPagoService,
            ILogger<EstadisticasController> logger)
        {
            _context = context;
            _gastosService = gastosService;
            _liquidacionService = liquidacionService;
            _planPagoService = planPagoService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard principal con estadísticas del mes actual
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardResponse>> ObtenerDashboard()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var fechaActual = DateTime.Now;
            var mes = fechaActual.Month;
            var año = fechaActual.Year;

            _logger.LogInformation("Generando dashboard para usuario {UserId} en grupo {GrupoId}", userId, grupoId);

            try
            {
                // Obtener datos principales
                var gastos = await _gastosService.ObtenerGastosMesGrupoAsync(grupoId, mes, año);
                var resumen = await _gastosService.ObtenerResumenMensualGrupoAsync(grupoId, mes, año);
                var estadoLiquidacion = await _liquidacionService.ObtenerEstadoMensualAsync(grupoId, mes, año);
                var cuotasPendientes = await _planPagoService.ObtenerCuotasPendientesAsync(userId);

                // Estadísticas del usuario actual
                var gastosUsuario = gastos.Where(g => g.UsuarioId == userId).ToList();
                var totalGastadoUsuario = gastosUsuario.Sum(g => g.Importe);
                var gastosDetalle = gastosUsuario.Where(g => g.EsDetalle).Sum(g => g.Importe);
                var gastosCompartidos = totalGastadoUsuario - gastosDetalle;

                // Categorías más usadas por el usuario
                var categoriasFavoritas = gastosUsuario
                    .GroupBy(g => new { g.CategoriaId, g.NombreCategoria, g.EmojiCategoria })
                    .Select(g => new CategoriaFavorita
                    {
                        CategoriaId = g.Key.CategoriaId,
                        Nombre = g.Key.NombreCategoria,
                        Emoji = g.Key.EmojiCategoria,
                        TotalGastos = g.Count(),
                        TotalImporte = g.Sum(x => x.Importe)
                    })
                    .OrderByDescending(c => c.TotalImporte)
                    .Take(5)
                    .ToList();

                // Tendencias semanales
                var tendenciasSemanal = gastos
                    .GroupBy(g => g.Fecha.Date.AddDays(-(int)g.Fecha.DayOfWeek))
                    .Select(g => new TendenciaSemanal
                    {
                        InicioSemana = g.Key,
                        TotalGastos = g.Count(),
                        TotalImporte = g.Sum(x => x.Importe),
                        PromedioGasto = g.Average(x => x.Importe)
                    })
                    .OrderBy(t => t.InicioSemana)
                    .ToList();

                // Comparación con mes anterior
                var mesAnterior = fechaActual.AddMonths(-1);
                var gastosMesAnterior = await _gastosService.ObtenerGastosMesGrupoAsync(grupoId, mesAnterior.Month, mesAnterior.Year);
                var totalMesAnterior = gastosMesAnterior.Sum(g => g.Importe);
                var diferenciaMensual = totalGastadoUsuario - gastosMesAnterior.Where(g => g.UsuarioId == userId).Sum(g => g.Importe);

                var dashboard = new DashboardResponse
                {
                    // Información básica
                    Mes = mes,
                    Año = año,
                    UsuarioId = userId,
                    GrupoId = grupoId,

                    // Estadísticas del mes actual
                    TotalGastosGrupo = gastos.Count,
                    TotalImporteGrupo = gastos.Sum(g => g.Importe),
                    TotalGastosUsuario = gastosUsuario.Count,
                    TotalImporteUsuario = totalGastadoUsuario,
                    GastosDetalleUsuario = gastosDetalle,
                    GastosCompartidosUsuario = gastosCompartidos,
                    PromedioGastoUsuario = gastosUsuario.Count > 0 ? gastosUsuario.Average(g => g.Importe) : 0,

                    // Estado de liquidación
                    RequiereLiquidacion = resumen.MontoDeuda > 0,
                    MontoDeuda = resumen.MontoDeuda,
                    EsDeudor = resumen.DeudorId == userId,
                    NombreOtraParte = resumen.DeudorId == userId ? resumen.NombreAcreedor : resumen.NombreDeudor,
                    TodosConfirmaron = resumen.TodosConfirmaron,
                    EstadoPago = estadoLiquidacion?.EstadoPago ?? "pendiente",

                    // Planes de pago
                    CuotasPendientes = cuotasPendientes.Count,
                    ProximaCuotaFecha = cuotasPendientes.OrderBy(c => c.FechaVencimiento).FirstOrDefault()?.FechaVencimiento,
                    CuotasVencidas = cuotasPendientes.Count(c => c.EstaVencida),

                    // Análisis y tendencias
                    CategoriasFavoritas = categoriasFavoritas,
                    TendenciasSemanal = tendenciasSemanal,
                    ComparacionMesAnterior = new ComparacionMensual
                    {
                        TotalMesAnterior = totalMesAnterior,
                        TotalMesActual = gastos.Sum(g => g.Importe),
                        DiferenciaMonto = diferenciaMensual,
                        PorcentajeCambio = totalMesAnterior > 0 ? Math.Round((diferenciaMensual / totalMesAnterior) * 100, 1) : 0
                    },

                    // Información contextual
                    UltimaActualizacion = DateTime.UtcNow,
                    PuedeConfirmarGastos = await _liquidacionService.PuedeConfirmarAsync(userId, mes, año)
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar dashboard para usuario {UserId}", userId);
                return StatusCode(500, "Error interno al generar el dashboard");
            }
        }

        /// <summary>
        /// Estadísticas anuales del grupo
        /// </summary>
        [HttpGet("anuales/{año}")]
        public async Task<ActionResult<EstadisticasAnualesResponse>> ObtenerEstadisticasAnuales(int año)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            _logger.LogInformation("Generando estadísticas anuales {Año} para grupo {GrupoId}", año, grupoId);

            var gastosPorMes = new Dictionary<int, decimal>();
            var gastosPorCategoria = new Dictionary<string, decimal>();
            var gastosPorUsuario = new Dictionary<string, decimal>();

            // Obtener datos de todos los meses
            for (int mes = 1; mes <= 12; mes++)
            {
                var gastosMes = await _gastosService.ObtenerGastosMesGrupoAsync(grupoId, mes, año);
                gastosPorMes[mes] = gastosMes.Sum(g => g.Importe);

                // Agrupar por categorías
                foreach (var grupo in gastosMes.GroupBy(g => g.NombreCategoria))
                {
                    if (!gastosPorCategoria.ContainsKey(grupo.Key))
                        gastosPorCategoria[grupo.Key] = 0;
                    gastosPorCategoria[grupo.Key] += grupo.Sum(g => g.Importe);
                }

                // Agrupar por usuarios
                foreach (var grupo in gastosMes.GroupBy(g => g.NombreUsuario))
                {
                    if (!gastosPorUsuario.ContainsKey(grupo.Key))
                        gastosPorUsuario[grupo.Key] = 0;
                    gastosPorUsuario[grupo.Key] += grupo.Sum(g => g.Importe);
                }
            }

            var response = new EstadisticasAnualesResponse
            {
                Año = año,
                GrupoId = grupoId,
                TotalAnual = gastosPorMes.Values.Sum(),
                PromedioMensual = gastosPorMes.Values.Average(),
                MesMayorGasto = gastosPorMes.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key,
                MesMenorGasto = gastosPorMes.Where(kvp => kvp.Value > 0).OrderBy(kvp => kvp.Value).FirstOrDefault().Key,
                GastosPorMes = gastosPorMes,
                GastosPorCategoria = gastosPorCategoria.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                GastosPorUsuario = gastosPorUsuario.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };

            return Ok(response);
        }

        /// <summary>
        /// Comparación entre múltiples períodos
        /// </summary>
        [HttpGet("comparacion")]
        public async Task<ActionResult<ComparacionPeriodosResponse>> CompararPeriodos(
            [FromQuery] int mesInicio, [FromQuery] int añoInicio,
            [FromQuery] int mesFin, [FromQuery] int añoFin)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var fechaInicio = new DateTime(añoInicio, mesInicio, 1);
            var fechaFin = new DateTime(añoFin, mesFin, 1);

            if (fechaInicio > fechaFin)
            {
                return BadRequest("La fecha de inicio debe ser anterior a la fecha de fin");
            }

            var periodos = new List<PeriodoComparacion>();
            var fechaActual = fechaInicio;

            while (fechaActual <= fechaFin)
            {
                var gastos = await _gastosService.ObtenerGastosMesGrupoAsync(grupoId, fechaActual.Month, fechaActual.Year);

                periodos.Add(new PeriodoComparacion
                {
                    Mes = fechaActual.Month,
                    Año = fechaActual.Year,
                    TotalGastos = gastos.Count,
                    TotalImporte = gastos.Sum(g => g.Importe),
                    GastosCompartidos = gastos.Where(g => !g.EsDetalle).Sum(g => g.Importe),
                    GastosDetalle = gastos.Where(g => g.EsDetalle).Sum(g => g.Importe)
                });

                fechaActual = fechaActual.AddMonths(1);
            }

            var response = new ComparacionPeriodosResponse
            {
                GrupoId = grupoId,
                Periodos = periodos,
                TotalGeneral = periodos.Sum(p => p.TotalImporte),
                PromedioMensual = periodos.Count > 0 ? periodos.Average(p => p.TotalImporte) : 0,
                MejorMes = periodos.OrderBy(p => p.TotalImporte).FirstOrDefault(),
                PeorMes = periodos.OrderByDescending(p => p.TotalImporte).FirstOrDefault()
            };

            return Ok(response);
        }

        /// <summary>
        /// Ranking de usuarios por diferentes métricas
        /// </summary>
        [HttpGet("ranking")]
        public async Task<ActionResult<RankingUsuariosResponse>> ObtenerRanking(
            [FromQuery] int? mes = null, [FromQuery] int? año = null)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var fechaActual = DateTime.Now;
            var mesConsulta = mes ?? fechaActual.Month;
            var añoConsulta = año ?? fechaActual.Year;

            var gastos = await _gastosService.ObtenerGastosMesGrupoAsync(grupoId, mesConsulta, añoConsulta);

            var rankingPorGasto = gastos
                .GroupBy(g => new { g.UsuarioId, g.NombreUsuario, g.ColorUsuario })
                .Select(g => new UsuarioRanking
                {
                    UsuarioId = g.Key.UsuarioId,
                    Nombre = g.Key.NombreUsuario,
                    Color = g.Key.ColorUsuario,
                    TotalGastos = g.Count(),
                    TotalImporte = g.Sum(x => x.Importe),
                    GastosCompartidos = g.Where(x => !x.EsDetalle).Sum(x => x.Importe),
                    GastosDetalle = g.Where(x => x.EsDetalle).Sum(x => x.Importe),
                    PromedioGasto = g.Average(x => x.Importe)
                })
                .OrderByDescending(u => u.TotalImporte)
                .ToList();

            // Asignar posiciones
            for (int i = 0; i < rankingPorGasto.Count; i++)
            {
                rankingPorGasto[i].Posicion = i + 1;
            }

            var response = new RankingUsuariosResponse
            {
                Mes = mesConsulta,
                Año = añoConsulta,
                GrupoId = grupoId,
                RankingPorImporte = rankingPorGasto,
                RankingPorCantidad = rankingPorGasto.OrderByDescending(u => u.TotalGastos).ToList(),
                RankingPorPromedio = rankingPorGasto.OrderByDescending(u => u.PromedioGasto).ToList(),
                TotalParticipantes = rankingPorGasto.Count
            };

            return Ok(response);
        }

        /// <summary>
        /// Análisis de patrones de gasto
        /// </summary>
        [HttpGet("patrones")]
        public async Task<ActionResult<PatronesGastoResponse>> AnalizarPatrones([FromQuery] int meses = 6)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var fechaActual = DateTime.Now;
            var patronesPorDia = new Dictionary<DayOfWeek, decimal>();
            var patronesPorCategoria = new Dictionary<string, List<decimal>>();
            var tendenciaMensual = new List<TendenciaMensual>();

            // Analizar últimos N meses
            for (int i = 0; i < meses; i++)
            {
                var fecha = fechaActual.AddMonths(-i);
                var gastos = await _gastosService.ObtenerGastosMesGrupoAsync(grupoId, fecha.Month, fecha.Year);

                // Tendencia mensual
                tendenciaMensual.Add(new TendenciaMensual
                {
                    Mes = fecha.Month,
                    Año = fecha.Year,
                    TotalImporte = gastos.Sum(g => g.Importe),
                    TotalGastos = gastos.Count,
                    CategoriasMasUsadas = gastos
                        .GroupBy(g => g.NombreCategoria)
                        .OrderByDescending(g => g.Sum(x => x.Importe))
                        .Take(3)
                        .Select(g => g.Key)
                        .ToList()
                });

                // Patrones por día de la semana
                foreach (var gasto in gastos)
                {
                    var diaSemana = gasto.Fecha.DayOfWeek;
                    if (!patronesPorDia.ContainsKey(diaSemana))
                        patronesPorDia[diaSemana] = 0;
                    patronesPorDia[diaSemana] += gasto.Importe;
                }

                // Patrones por categoría
                foreach (var grupo in gastos.GroupBy(g => g.NombreCategoria))
                {
                    if (!patronesPorCategoria.ContainsKey(grupo.Key))
                        patronesPorCategoria[grupo.Key] = new List<decimal>();
                    patronesPorCategoria[grupo.Key].Add(grupo.Sum(g => g.Importe));
                }
            }

            var response = new PatronesGastoResponse
            {
                GrupoId = grupoId,
                MesesAnalizados = meses,
                PatronesPorDia = patronesPorDia.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
                CategoriasEstables = patronesPorCategoria
                    .Where(kvp => kvp.Value.Count >= meses / 2) // Aparece en al menos la mitad de los meses
                    .OrderByDescending(kvp => kvp.Value.Average())
                    .Take(5)
                    .ToDictionary(kvp => kvp.Key, kvp => new CategoriaPatron
                    {
                        PromedioMensual = kvp.Value.Average(),
                        Variabilidad = CalcularVariabilidad(kvp.Value),
                        Tendencia = CalcularTendencia(kvp.Value)
                    }),
                TendenciaMensual = tendenciaMensual.OrderBy(t => new DateTime(t.Año, t.Mes, 1)).ToList(),
                DiaMasActivo = patronesPorDia.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key.ToString(),
                DiaMenosActivo = patronesPorDia.OrderBy(kvp => kvp.Value).FirstOrDefault().Key.ToString()
            };

            return Ok(response);
        }

        /// <summary>
        /// Exportar datos para análisis externo
        /// </summary>
        [HttpGet("exportar")]
        public async Task<ActionResult> ExportarDatos(
            [FromQuery] int mesInicio, [FromQuery] int añoInicio,
            [FromQuery] int mesFin, [FromQuery] int añoFin,
            [FromQuery] string formato = "csv")
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            // Obtener todos los gastos del período
            var todosLosGastos = new List<object>();
            var fechaInicio = new DateTime(añoInicio, mesInicio, 1);
            var fechaFin = new DateTime(añoFin, mesFin, 1);
            var fechaActual = fechaInicio;

            while (fechaActual <= fechaFin)
            {
                var gastos = await _gastosService.ObtenerGastosMesGrupoAsync(grupoId, fechaActual.Month, fechaActual.Year);
                todosLosGastos.AddRange(gastos.Select(g => new
                {
                    Fecha = g.Fecha.ToString("yyyy-MM-dd"),
                    Usuario = g.NombreUsuario,
                    Categoria = g.NombreCategoria,
                    Descripcion = g.Descripcion,
                    Importe = g.Importe,
                    TipoGasto = g.EsDetalle ? "Detalle" : "Compartido",
                    Tienda = g.Tienda ?? "",
                    Mes = fechaActual.Month,
                    Año = fechaActual.Year
                }));

                fechaActual = fechaActual.AddMonths(1);
            }

            if (formato.ToLower() == "csv")
            {
                var csv = GenerarCSV(todosLosGastos);
                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"gastos_{grupoId}_{añoInicio}{mesInicio:D2}_{añoFin}{mesFin:D2}.csv");
            }

            return Ok(todosLosGastos);
        }

        // ===== MÉTODOS AUXILIARES =====

        private static decimal CalcularVariabilidad(List<decimal> valores)
        {
            if (valores.Count < 2) return 0;

            var promedio = valores.Average();
            var varianza = valores.Sum(v => Math.Pow((double)(v - promedio), 2)) / valores.Count;
            return (decimal)Math.Sqrt(varianza);
        }

        private static string CalcularTendencia(List<decimal> valores)
        {
            if (valores.Count < 2) return "Estable";

            var primeraMitad = valores.Take(valores.Count / 2).Average();
            var segundaMitad = valores.Skip(valores.Count / 2).Average();

            var diferencia = ((segundaMitad - primeraMitad) / primeraMitad) * 100;

            return diferencia switch
            {
                > 10 => "Creciente",
                < -10 => "Decreciente",
                _ => "Estable"
            };
        }

        private static string GenerarCSV(List<object> datos)
        {
            if (!datos.Any()) return "Sin datos";

            var propiedades = datos.First().GetType().GetProperties();
            var csv = new System.Text.StringBuilder();

            // Encabezados
            csv.AppendLine(string.Join(",", propiedades.Select(p => p.Name)));

            // Datos
            foreach (var item in datos)
            {
                var valores = propiedades.Select(p =>
                {
                    var valor = p.GetValue(item)?.ToString() ?? "";
                    return valor.Contains(",") ? $"\"{valor}\"" : valor;
                });
                csv.AppendLine(string.Join(",", valores));
            }

            return csv.ToString();
        }
    }

    // ===== DTOs PARA RESPUESTAS =====

    public class DashboardResponse
    {
        public int Mes { get; set; }
        public int Año { get; set; }
        public int UsuarioId { get; set; }
        public int GrupoId { get; set; }

        public int TotalGastosGrupo { get; set; }
        public decimal TotalImporteGrupo { get; set; }
        public int TotalGastosUsuario { get; set; }
        public decimal TotalImporteUsuario { get; set; }
        public decimal GastosDetalleUsuario { get; set; }
        public decimal GastosCompartidosUsuario { get; set; }
        public decimal PromedioGastoUsuario { get; set; }

        public bool RequiereLiquidacion { get; set; }
        public decimal MontoDeuda { get; set; }
        public bool EsDeudor { get; set; }
        public string? NombreOtraParte { get; set; }
        public bool TodosConfirmaron { get; set; }
        public string EstadoPago { get; set; } = string.Empty;

        public int CuotasPendientes { get; set; }
        public DateTime? ProximaCuotaFecha { get; set; }
        public int CuotasVencidas { get; set; }

        public List<CategoriaFavorita> CategoriasFavoritas { get; set; } = new();
        public List<TendenciaSemanal> TendenciasSemanal { get; set; } = new();
        public ComparacionMensual ComparacionMesAnterior { get; set; } = new();

        public DateTime UltimaActualizacion { get; set; }
        public bool PuedeConfirmarGastos { get; set; }
    }

    public class CategoriaFavorita
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public int TotalGastos { get; set; }
        public decimal TotalImporte { get; set; }
    }

    public class TendenciaSemanal
    {
        public DateTime InicioSemana { get; set; }
        public int TotalGastos { get; set; }
        public decimal TotalImporte { get; set; }
        public decimal PromedioGasto { get; set; }
    }

    public class ComparacionMensual
    {
        public decimal TotalMesAnterior { get; set; }
        public decimal TotalMesActual { get; set; }
        public decimal DiferenciaMonto { get; set; }
        public decimal PorcentajeCambio { get; set; }
    }

    public class EstadisticasAnualesResponse
    {
        public int Año { get; set; }
        public int GrupoId { get; set; }
        public decimal TotalAnual { get; set; }
        public decimal PromedioMensual { get; set; }
        public int MesMayorGasto { get; set; }
        public int MesMenorGasto { get; set; }
        public Dictionary<int, decimal> GastosPorMes { get; set; } = new();
        public Dictionary<string, decimal> GastosPorCategoria { get; set; } = new();
        public Dictionary<string, decimal> GastosPorUsuario { get; set; } = new();
    }

    public class ComparacionPeriodosResponse
    {
        public int GrupoId { get; set; }
        public List<PeriodoComparacion> Periodos { get; set; } = new();
        public decimal TotalGeneral { get; set; }
        public decimal PromedioMensual { get; set; }
        public PeriodoComparacion? MejorMes { get; set; }
        public PeriodoComparacion? PeorMes { get; set; }
    }

    public class PeriodoComparacion
    {
        public int Mes { get; set; }
        public int Año { get; set; }
        public int TotalGastos { get; set; }
        public decimal TotalImporte { get; set; }
        public decimal GastosCompartidos { get; set; }
        public decimal GastosDetalle { get; set; }
    }

    public class RankingUsuariosResponse
    {
        public int Mes { get; set; }
        public int Año { get; set; }
        public int GrupoId { get; set; }
        public List<UsuarioRanking> RankingPorImporte { get; set; } = new();
        public List<UsuarioRanking> RankingPorCantidad { get; set; } = new();
        public List<UsuarioRanking> RankingPorPromedio { get; set; } = new();
        public int TotalParticipantes { get; set; }
    }

    public class UsuarioRanking
    {
        public int Posicion { get; set; }
        public int UsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int TotalGastos { get; set; }
        public decimal TotalImporte { get; set; }
        public decimal GastosCompartidos { get; set; }
        public decimal GastosDetalle { get; set; }
        public decimal PromedioGasto { get; set; }
    }

    public class PatronesGastoResponse
    {
        public int GrupoId { get; set; }
        public int MesesAnalizados { get; set; }
        public Dictionary<string, decimal> PatronesPorDia { get; set; } = new();
        public Dictionary<string, CategoriaPatron> CategoriasEstables { get; set; } = new();
        public List<TendenciaMensual> TendenciaMensual { get; set; } = new();
        public string DiaMasActivo { get; set; } = string.Empty;
        public string DiaMenosActivo { get; set; } = string.Empty;
    }

    public class CategoriaPatron
    {
        public decimal PromedioMensual { get; set; }
        public decimal Variabilidad { get; set; }
        public string Tendencia { get; set; } = string.Empty;
    }

    public class TendenciaMensual
    {
        public int Mes { get; set; }
        public int Año { get; set; }
        public decimal TotalImporte { get; set; }
        public int TotalGastos { get; set; }
        public List<string> CategoriasMasUsadas { get; set; } = new();
    }
}