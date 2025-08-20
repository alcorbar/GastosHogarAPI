using GastosHogarAPI.Models.DTOs;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastosHogarAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GastosController : ControllerBase
    {
        private readonly IGastosService _gastosService;
        private readonly ILogger<GastosController> _logger;

        public GastosController(IGastosService gastosService, ILogger<GastosController> logger)
        {
            _gastosService = gastosService;
            _logger = logger;
        }

        /// <summary>
        /// Crear un nuevo gasto
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CrearGastoResponse>> CrearGasto([FromBody] CrearGastoRequest request)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // Asegurar que el gasto se asigne al usuario autenticado
            request.UsuarioId = userId;

            // Verificar que el usuario tiene acceso al grupo
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId) || grupoId != request.GrupoId)
            {
                return Forbid("No tienes acceso a este grupo");
            }

            _logger.LogInformation("Creando gasto para usuario {UserId} en grupo {GrupoId}: €{Importe} - {Descripcion}",
                userId, request.GrupoId, request.Importe, request.Descripcion);

            var gastoId = await _gastosService.CrearGastoAsync(request);

            return Ok(new CrearGastoResponse
            {
                Exito = true,
                GastoId = gastoId,
                Mensaje = "Gasto creado exitosamente"
            });
        }

        /// <summary>
        /// Obtener gastos del mes para el grupo del usuario
        /// </summary>
        [HttpGet("mes/{mes}/año/{año}")]
        public async Task<ActionResult<List<GastoResponse>>> ObtenerGastosMes(int mes, int año)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            _logger.LogInformation("Obteniendo gastos para grupo {GrupoId}, mes {Mes}/{Año}", grupoId, mes, año);

            var gastos = await _gastosService.ObtenerGastosMesGrupoAsync(grupoId, mes, año);
            return Ok(gastos);
        }

        /// <summary>
        /// Obtener todos los gastos (método legacy - mantener compatibilidad)
        /// </summary>
        [HttpGet("todos/mes/{mes}/año/{año}")]
        public async Task<ActionResult<List<object>>> ObtenerTodosGastosMes(int mes, int año)
        {
            _logger.LogInformation("Obteniendo todos los gastos para mes {Mes}/{Año}", mes, año);

            var gastos = await _gastosService.ObtenerGastosMesAsync(mes, año);
            return Ok(gastos);
        }

        /// <summary>
        /// Obtener resumen mensual del grupo
        /// </summary>
        [HttpGet("resumen/mes/{mes}/año/{año}")]
        public async Task<ActionResult<ResumenMensual>> ObtenerResumenMensual(int mes, int año)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            _logger.LogInformation("Obteniendo resumen mensual para grupo {GrupoId}, mes {Mes}/{Año}", grupoId, mes, año);

            var resumen = await _gastosService.ObtenerResumenMensualGrupoAsync(grupoId, mes, año);
            return Ok(resumen);
        }

        /// <summary>
        /// Obtener resumen mensual global (método legacy)
        /// </summary>
        [HttpGet("resumen-global/mes/{mes}/año/{año}")]
        public async Task<ActionResult<ResumenMensual>> ObtenerResumenMensualGlobal(int mes, int año)
        {
            _logger.LogInformation("Obteniendo resumen mensual global para mes {Mes}/{Año}", mes, año);

            var resumen = await _gastosService.ObtenerResumenMensualAsync(mes, año);
            return Ok(resumen);
        }

        /// <summary>
        /// Subir foto de ticket (placeholder)
        /// </summary>
        [HttpPost("{gastoId}/foto")]
        public ActionResult SubirFoto(int gastoId, IFormFile foto)
        {
            if (foto == null || foto.Length == 0)
            {
                return BadRequest("No se ha proporcionado una foto válida");
            }

            // TODO: Implementar subida de fotos
            _logger.LogInformation("Subida de foto para gasto {GastoId} - Tamaño: {Size} bytes", gastoId, foto.Length);

            return Ok(new { mensaje = "Funcionalidad de fotos en desarrollo" });
        }

        /// <summary>
        /// Obtener estadísticas rápidas del mes actual
        /// </summary>
        [HttpGet("estadisticas-mes-actual")]
        public async Task<ActionResult<EstadisticasRapidas>> ObtenerEstadisticasMesActual()
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var fechaActual = DateTime.Now;
            var gastos = await _gastosService.ObtenerGastosMesGrupoAsync(grupoId, fechaActual.Month, fechaActual.Year);
            var resumen = await _gastosService.ObtenerResumenMensualGrupoAsync(grupoId, fechaActual.Month, fechaActual.Year);

            return Ok(new EstadisticasRapidas
            {
                TotalGastos = gastos.Count,
                ImporteTotal = gastos.Sum(g => g.Importe),
                PromedioGasto = gastos.Count > 0 ? gastos.Average(g => g.Importe) : 0,
                GastoMayor = gastos.Count > 0 ? gastos.Max(g => g.Importe) : 0,
                PendienteLiquidacion = resumen.MontoDeuda,
                TodosConfirmaron = resumen.TodosConfirmaron
            });
        }
    }

    // DTOs adicionales
    public class CrearGastoResponse
    {
        public bool Exito { get; set; }
        public int GastoId { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class EstadisticasRapidas
    {
        public int TotalGastos { get; set; }
        public decimal ImporteTotal { get; set; }
        public decimal PromedioGasto { get; set; }
        public decimal GastoMayor { get; set; }
        public decimal PendienteLiquidacion { get; set; }
        public bool TodosConfirmaron { get; set; }
    }
}