using GastosHogarAPI.Models.DTOs;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastosHogarAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PlanPagoController : ControllerBase
    {
        private readonly IPlanPagoService _planPagoService;
        private readonly ILogger<PlanPagoController> _logger;

        public PlanPagoController(IPlanPagoService planPagoService, ILogger<PlanPagoController> logger)
        {
            _planPagoService = planPagoService;
            _logger = logger;
        }

        /// <summary>
        /// Crear un nuevo plan de pago
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<int>> CrearPlanPago([FromBody] CrearPlanPagoRequest request)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            // Verificar que el grupo coincide
            if (request.GrupoId != grupoId)
            {
                return Forbid("No tienes acceso a este grupo");
            }

            _logger.LogInformation("Creando plan de pago para grupo {GrupoId}: €{MontoTotal} - {NumeroCuotas} cuotas",
                grupoId, request.MontoTotal, request.NumeroCuotas);

            try
            {
                var planId = await _planPagoService.CrearPlanPagoAsync(request);
                return Ok(new { planId, mensaje = "Plan de pago creado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear plan de pago");
                return BadRequest("Error al crear el plan de pago");
            }
        }

        /// <summary>
        /// Obtener planes de pago del grupo
        /// </summary>
        [HttpGet("grupo")]
        public async Task<ActionResult<List<PlanPagoResponse>>> ObtenerPlanesGrupo()
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            _logger.LogInformation("Obteniendo planes de pago para grupo {GrupoId}", grupoId);

            var planes = await _planPagoService.ObtenerPlanesPorGrupoAsync(grupoId);
            return Ok(planes);
        }

        /// <summary>
        /// Obtener planes de pago del usuario actual
        /// </summary>
        [HttpGet("mis-planes")]
        public async Task<ActionResult<List<PlanPagoResponse>>> ObtenerMisPlanes()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Obteniendo planes de pago para usuario {UserId}", userId);

            var planes = await _planPagoService.ObtenerPlanesPorUsuarioAsync(userId);
            return Ok(planes);
        }

        /// <summary>
        /// Obtener detalle completo de un plan de pago
        /// </summary>
        [HttpGet("{planId}")]
        public async Task<ActionResult<PlanPagoDetalleResponse>> ObtenerDetallePlan(int planId)
        {
            _logger.LogInformation("Obteniendo detalle del plan {PlanId}", planId);

            var detalle = await _planPagoService.ObtenerDetallePlanAsync(planId);

            if (detalle == null)
            {
                return NotFound("Plan de pago no encontrado");
            }

            return Ok(detalle);
        }

        /// <summary>
        /// Pagar una cuota específica
        /// </summary>
        [HttpPost("cuotas/pagar")]
        public async Task<ActionResult> PagarCuota([FromBody] PagarCuotaRequest request)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Usuario {UserId} pagando cuota {CuotaId} via {MetodoPago}",
                userId, request.CuotaId, request.MetodoPago);

            try
            {
                var exito = await _planPagoService.PagarCuotaAsync(request);

                if (!exito)
                {
                    return BadRequest("Error al procesar el pago de la cuota");
                }

                return Ok(new
                {
                    exito = true,
                    mensaje = "Cuota pagada exitosamente",
                    cuotaId = request.CuotaId,
                    metodoPago = request.MetodoPago
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al pagar cuota {CuotaId}", request.CuotaId);
                return BadRequest("Error interno al procesar el pago");
            }
        }

        /// <summary>
        /// Confirmar recepción de una cuota (para el acreedor)
        /// </summary>
        [HttpPost("cuotas/{cuotaId}/confirmar")]
        public async Task<ActionResult> ConfirmarCuota(int cuotaId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Usuario {UserId} confirmando cuota {CuotaId}", userId, cuotaId);

            try
            {
                var exito = await _planPagoService.ConfirmarCuotaAsync(cuotaId, userId);

                if (!exito)
                {
                    return BadRequest("Error al confirmar la cuota o no tienes permisos");
                }

                return Ok(new
                {
                    exito = true,
                    mensaje = "Cuota confirmada exitosamente",
                    cuotaId = cuotaId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al confirmar cuota {CuotaId}", cuotaId);
                return BadRequest("Error interno al confirmar");
            }
        }

        /// <summary>
        /// Obtener cuotas pendientes del usuario
        /// </summary>
        [HttpGet("cuotas-pendientes")]
        public async Task<ActionResult<List<CuotaPendienteResponse>>> ObtenerCuotasPendientes()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Obteniendo cuotas pendientes para usuario {UserId}", userId);

            var cuotas = await _planPagoService.ObtenerCuotasPendientesAsync(userId);
            return Ok(cuotas);
        }

        /// <summary>
        /// Completar un plan de pago manualmente
        /// </summary>
        [HttpPost("{planId}/completar")]
        public async Task<ActionResult> CompletarPlan(int planId)
        {
            _logger.LogInformation("Completando plan {PlanId}", planId);

            try
            {
                var exito = await _planPagoService.CompletarPlanAsync(planId);

                if (!exito)
                {
                    return BadRequest("Error al completar el plan o plan no encontrado");
                }

                return Ok(new
                {
                    exito = true,
                    mensaje = "Plan de pago completado exitosamente",
                    planId = planId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al completar plan {PlanId}", planId);
                return BadRequest("Error interno al completar el plan");
            }
        }

        /// <summary>
        /// Cancelar un plan de pago
        /// </summary>
        [HttpPost("{planId}/cancelar")]
        public async Task<ActionResult> CancelarPlan(int planId)
        {
            _logger.LogInformation("Cancelando plan {PlanId}", planId);

            try
            {
                var exito = await _planPagoService.CancelarPlanAsync(planId);

                if (!exito)
                {
                    return BadRequest("Error al cancelar el plan o plan no encontrado");
                }

                return Ok(new
                {
                    exito = true,
                    mensaje = "Plan de pago cancelado exitosamente",
                    planId = planId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cancelar plan {PlanId}", planId);
                return BadRequest("Error interno al cancelar el plan");
            }
        }

        /// <summary>
        /// Obtener resumen de planes para el usuario
        /// </summary>
        [HttpGet("resumen")]
        public ActionResult<ResumenPlanesUsuarioResponse> ObtenerResumenPlanes()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Obteniendo resumen de planes para usuario {UserId}", userId);

            // Por ahora retornamos un resumen básico
            // En una implementación completa, esto estaría en el servicio
            return Ok(new ResumenPlanesUsuarioResponse
            {
                UsuarioId = userId,
                NombreUsuario = User.FindFirst("UserName")?.Value ?? "Usuario",
                // Aquí irían los datos reales del servicio
                PlanesActivos = 0,
                PlanesCompletados = 0,
                TotalDebo = 0,
                TotalMeDeben = 0,
                BalanceNeto = 0,
                CuotasPendientesPagar = 0,
                CuotasPendientesConfirmar = 0,
                ProximasCuotas = new List<CuotaPendienteResponse>()
            });
        }

        /// <summary>
        /// Obtener estadísticas de planes de pago del grupo
        /// </summary>
        [HttpGet("estadisticas")]
        public ActionResult ObtenerEstadisticas()
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            _logger.LogInformation("Obteniendo estadísticas de planes para grupo {GrupoId}", grupoId);

            // Implementación básica por ahora
            return Ok(new
            {
                grupoId = grupoId,
                totalPlanes = 0,
                planesActivos = 0,
                planesCompletados = 0,
                montoTotalPagos = 0m,
                proximosVencimientos = Array.Empty<object>(),
                mensaje = "Funcionalidad en desarrollo"
            });
        }
    }
}