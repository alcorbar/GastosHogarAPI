using GastosHogarAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastosHogarAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LiquidacionController : ControllerBase
    {
        private readonly ILiquidacionService _liquidacionService;
        private readonly ILogger<LiquidacionController> _logger;

        public LiquidacionController(ILiquidacionService liquidacionService, ILogger<LiquidacionController> logger)
        {
            _liquidacionService = liquidacionService;
            _logger = logger;
        }

        /// <summary>
        /// Confirmar gastos del mes actual
        /// </summary>
        [HttpPost("confirmar")]
        public async Task<ActionResult> ConfirmarGastos([FromBody] ConfirmarGastosRequest? request = null)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // Si no se especifica mes/año, usar el actual
            var mes = request?.Mes ?? DateTime.Now.Month;
            var año = request?.Año ?? DateTime.Now.Year;

            _logger.LogInformation("Usuario {UserId} confirmando gastos para {Mes}/{Año}", userId, mes, año);

            var exito = await _liquidacionService.ConfirmarGastosAsync(userId, mes, año);

            if (!exito)
            {
                return BadRequest(new { mensaje = "Error al confirmar gastos" });
            }

            _logger.LogInformation("Usuario {UserId} confirmó exitosamente gastos para {Mes}/{Año}", userId, mes, año);

            return Ok(new
            {
                exito = true,
                mensaje = "Gastos confirmados exitosamente",
                mes = mes,
                año = año
            });
        }

        /// <summary>
        /// Confirmar gastos para un mes específico
        /// </summary>
        [HttpPost("confirmar/{mes}/{año}")]
        public async Task<ActionResult> ConfirmarGastosMes(int mes, int año)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Usuario {UserId} confirmando gastos para {Mes}/{Año}", userId, mes, año);

            var exito = await _liquidacionService.ConfirmarGastosAsync(userId, mes, año);

            if (!exito)
            {
                return BadRequest(new { mensaje = "Error al confirmar gastos" });
            }

            return Ok(new
            {
                exito = true,
                mensaje = "Gastos confirmados exitosamente",
                mes = mes,
                año = año
            });
        }

        /// <summary>
        /// Obtener estado de liquidación actual
        /// </summary>
        [HttpGet("estado")]
        public async Task<ActionResult<EstadoLiquidacionResponse>> ObtenerEstado([FromQuery] int? mes = null, [FromQuery] int? año = null)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // Si no se especifica mes/año, usar el actual
            var mesConsulta = mes ?? DateTime.Now.Month;
            var añoConsulta = año ?? DateTime.Now.Year;

            _logger.LogInformation("Obteniendo estado de liquidación para usuario {UserId}, {Mes}/{Año}",
                userId, mesConsulta, añoConsulta);

            var estado = await _liquidacionService.ObtenerEstadoMensualPorUsuarioAsync(userId, mesConsulta, añoConsulta);
            var puedeConfirmar = await _liquidacionService.PuedeConfirmarAsync(userId, mesConsulta, añoConsulta);

            if (estado == null)
            {
                return Ok(new EstadoLiquidacionResponse
                {
                    ExisteEstado = false,
                    Mes = mesConsulta,
                    Año = añoConsulta,
                    PuedeConfirmar = true,
                    Mensaje = "No hay gastos registrados para este período"
                });
            }

            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            int.TryParse(grupoIdClaim, out var grupoId);

            var usuariosPendientes = await _liquidacionService.ObtenerUsuariosPendientesConfirmacionAsync(grupoId, mesConsulta, añoConsulta);
            var totalConfirmaciones = await _liquidacionService.ContarConfirmacionesAsync(grupoId, mesConsulta, añoConsulta);

            return Ok(new EstadoLiquidacionResponse
            {
                ExisteEstado = true,
                Mes = mesConsulta,
                Año = añoConsulta,
                MontoDeuda = estado.MontoDeuda,
                EstadoPago = estado.EstadoPago,
                TodosConfirmaron = estado.TodosConfirmaron,
                LiquidacionCalculada = estado.LiquidacionCalculada,
                FechaLiquidacion = estado.FechaLiquidacion,
                FechaPago = estado.FechaPago,
                MetodoPago = estado.MetodoPago,
                PuedeConfirmar = puedeConfirmar,
                UsuariosPendientes = usuariosPendientes.Count,
                TotalConfirmaciones = totalConfirmaciones,
                Mensaje = estado.TodosConfirmaron ? "Todos los usuarios han confirmado" : "Esperando confirmaciones"
            });
        }

        /// <summary>
        /// Marcar pago como realizado
        /// </summary>
        [HttpPost("marcar-pago")]
        public async Task<ActionResult> MarcarPagoRealizado([FromBody] MarcarPagoRequest request)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var mes = request.Mes ?? DateTime.Now.Month;
            var año = request.Año ?? DateTime.Now.Year;

            _logger.LogInformation("Marcando pago como realizado para grupo {GrupoId}, {Mes}/{Año} via {MetodoPago}",
                grupoId, mes, año, request.MetodoPago);

            var exito = await _liquidacionService.MarcarPagoRealizadoAsync(grupoId, mes, año, request.MetodoPago);

            if (!exito)
            {
                return BadRequest(new { mensaje = "Error al marcar pago o liquidación no calculada" });
            }

            return Ok(new
            {
                exito = true,
                mensaje = "Pago marcado como realizado",
                metodoPago = request.MetodoPago
            });
        }

        /// <summary>
        /// Confirmar pago recibido
        /// </summary>
        [HttpPost("confirmar-pago")]
        public async Task<ActionResult> ConfirmarPagoRecibido([FromBody] ConfirmarPagoRequest request)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var mes = request.Mes ?? DateTime.Now.Month;
            var año = request.Año ?? DateTime.Now.Year;

            _logger.LogInformation("Confirmando pago recibido para grupo {GrupoId}, {Mes}/{Año}",
                grupoId, mes, año);

            var exito = await _liquidacionService.ConfirmarPagoRecibidoAsync(grupoId, mes, año);

            if (!exito)
            {
                return BadRequest(new { mensaje = "Error al confirmar pago o pago no realizado aún" });
            }

            return Ok(new
            {
                exito = true,
                mensaje = "Pago confirmado como recibido"
            });
        }

        /// <summary>
        /// Verificar si el usuario puede confirmar gastos
        /// </summary>
        [HttpGet("puede-confirmar")]
        public async Task<ActionResult<PuedeConfirmarResponse>> PuedeConfirmar([FromQuery] int? mes = null, [FromQuery] int? año = null)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var mesConsulta = mes ?? DateTime.Now.Month;
            var añoConsulta = año ?? DateTime.Now.Year;

            var puedeConfirmar = await _liquidacionService.PuedeConfirmarAsync(userId, mesConsulta, añoConsulta);

            return Ok(new PuedeConfirmarResponse
            {
                PuedeConfirmar = puedeConfirmar,
                Mes = mesConsulta,
                Año = añoConsulta,
                Mensaje = puedeConfirmar ? "Puede confirmar gastos" : "Ya confirmó o liquidación cerrada"
            });
        }

        /// <summary>
        /// Obtener resumen de confirmaciones pendientes
        /// </summary>
        [HttpGet("pendientes")]
        public async Task<ActionResult<ConfirmacionesPendientesResponse>> ObtenerPendientes([FromQuery] int? mes = null, [FromQuery] int? año = null)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var mesConsulta = mes ?? DateTime.Now.Month;
            var añoConsulta = año ?? DateTime.Now.Year;

            var usuariosPendientes = await _liquidacionService.ObtenerUsuariosPendientesConfirmacionAsync(grupoId, mesConsulta, añoConsulta);
            var totalConfirmaciones = await _liquidacionService.ContarConfirmacionesAsync(grupoId, mesConsulta, añoConsulta);

            return Ok(new ConfirmacionesPendientesResponse
            {
                UsuariosPendientes = usuariosPendientes,
                TotalConfirmaciones = totalConfirmaciones,
                Mes = mesConsulta,
                Año = añoConsulta
            });
        }
    }

    // DTOs para el controlador
    public class ConfirmarGastosRequest
    {
        public int? Mes { get; set; }
        public int? Año { get; set; }
    }

    public class EstadoLiquidacionResponse
    {
        public bool ExisteEstado { get; set; }
        public int Mes { get; set; }
        public int Año { get; set; }
        public decimal MontoDeuda { get; set; }
        public string EstadoPago { get; set; } = string.Empty;
        public bool TodosConfirmaron { get; set; }
        public bool LiquidacionCalculada { get; set; }
        public DateTime? FechaLiquidacion { get; set; }
        public DateTime? FechaPago { get; set; }
        public string? MetodoPago { get; set; }
        public bool PuedeConfirmar { get; set; }
        public int UsuariosPendientes { get; set; }
        public int TotalConfirmaciones { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class MarcarPagoRequest
    {
        public int? Mes { get; set; }
        public int? Año { get; set; }
        public string MetodoPago { get; set; } = string.Empty;
    }

    public class ConfirmarPagoRequest
    {
        public int? Mes { get; set; }
        public int? Año { get; set; }
    }

    public class PuedeConfirmarResponse
    {
        public bool PuedeConfirmar { get; set; }
        public int Mes { get; set; }
        public int Año { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class ConfirmacionesPendientesResponse
    {
        public List<int> UsuariosPendientes { get; set; } = new();
        public int TotalConfirmaciones { get; set; }
        public int Mes { get; set; }
        public int Año { get; set; }
    }
}