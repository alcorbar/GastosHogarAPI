using GastosHogarAPI.Models.DTOs;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastosHogarAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GruposController : ControllerBase
    {
        private readonly IGrupoService _grupoService;
        private readonly ILogger<GruposController> _logger;

        public GruposController(IGrupoService grupoService, ILogger<GruposController> logger)
        {
            _grupoService = grupoService;
            _logger = logger;
        }

        /// <summary>
        /// Crear un nuevo grupo
        /// </summary>
        [HttpPost("crear")]
        public async Task<ActionResult<GrupoResponse>> CrearGrupo([FromBody] CrearGrupoRequest request)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // Asegurar que el grupo se asigne al usuario autenticado
            request.UsuarioId = userId;

            _logger.LogInformation("Usuario {UserId} creando grupo: {NombreGrupo}", userId, request.Nombre);

            var response = await _grupoService.CrearGrupoAsync(request);

            if (!response.Exito)
            {
                _logger.LogWarning("Error al crear grupo para usuario {UserId}: {Mensaje}", userId, response.Mensaje);
                return BadRequest(response);
            }

            _logger.LogInformation("Grupo {GrupoId} '{NombreGrupo}' creado exitosamente por usuario {UserId}",
                response.GrupoId, response.NombreGrupo, userId);

            return Ok(response);
        }

        /// <summary>
        /// Unirse a un grupo usando código de invitación
        /// </summary>
        [HttpPost("unirse-codigo")]
        public async Task<ActionResult<GrupoResponse>> UnirseGrupoCodigo([FromBody] UnirseGrupoRequest request)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // Asegurar que la solicitud corresponde al usuario autenticado
            request.UsuarioId = userId;

            _logger.LogInformation("Usuario {UserId} intentando unirse a grupo con código: {Codigo}",
                userId, request.CodigoInvitacion);

            var response = await _grupoService.UnirseGrupoAsync(request);

            if (!response.Exito)
            {
                _logger.LogWarning("Error al unirse a grupo con código {Codigo}: {Mensaje}",
                    request.CodigoInvitacion, response.Mensaje);
                return BadRequest(response);
            }

            _logger.LogInformation("Usuario {UserId} se unió exitosamente al grupo {GrupoId}",
                userId, response.GrupoId);

            return Ok(response);
        }

        /// <summary>
        /// Unirse al grupo de otro usuario
        /// </summary>
        [HttpPost("unirse-usuario")]
        public async Task<ActionResult<GrupoResponse>> UnirseGrupoUsuario([FromBody] UnirseGrupoUsuarioRequest request)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            // Asegurar que la solicitud corresponde al usuario autenticado
            request.UsuarioId = userId;

            _logger.LogInformation("Usuario {UserId} intentando unirse al grupo del usuario {UsuarioDestinoId}",
                userId, request.UsuarioDestinoId);

            var response = await _grupoService.UnirseGrupoUsuarioAsync(request);

            if (!response.Exito)
            {
                _logger.LogWarning("Error al unirse al grupo del usuario {UsuarioDestinoId}: {Mensaje}",
                    request.UsuarioDestinoId, response.Mensaje);
                return BadRequest(response);
            }

            _logger.LogInformation("Usuario {UserId} se unió exitosamente al grupo {GrupoId} del usuario {UsuarioDestinoId}",
                userId, response.GrupoId, request.UsuarioDestinoId);

            return Ok(response);
        }

        /// <summary>
        /// Salir del grupo actual
        /// </summary>
        [HttpPost("salir")]
        public async Task<ActionResult> SalirGrupo()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            _logger.LogInformation("Usuario {UserId} saliendo de su grupo", userId);

            var exito = await _grupoService.SalirGrupoAsync(userId);

            if (!exito)
            {
                return BadRequest(new { mensaje = "Error al salir del grupo o no perteneces a ninguno" });
            }

            _logger.LogInformation("Usuario {UserId} salió exitosamente de su grupo", userId);

            return Ok(new
            {
                exito = true,
                mensaje = "Has salido del grupo exitosamente"
            });
        }

        /// <summary>
        /// Obtener miembros del grupo actual
        /// </summary>
        [HttpGet("miembros")]
        public async Task<ActionResult<List<MiembroGrupo>>> ObtenerMiembros()
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest(new { mensaje = "Usuario no pertenece a ningún grupo" });
            }

            _logger.LogInformation("Obteniendo miembros del grupo {GrupoId}", grupoId);

            var miembros = await _grupoService.ObtenerMiembrosGrupoAsync(grupoId);
            return Ok(miembros);
        }

        /// <summary>
        /// Obtener información del grupo actual
        /// </summary>
        [HttpGet("mi-grupo")]
        public ActionResult<InfoGrupoActual> ObtenerMiGrupo()
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            var userIdClaim = User.FindFirst("UserId")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return Ok(new InfoGrupoActual
                {
                    TieneGrupo = false,
                    Mensaje = "No perteneces a ningún grupo"
                });
            }

            return Ok(new InfoGrupoActual
            {
                TieneGrupo = true,
                GrupoId = grupoId,
                UsuarioId = userId,
                Mensaje = "Información del grupo obtenida"
            });
        }

        /// <summary>
        /// Regenerar código de invitación (solo para creadores)
        /// </summary>
        [HttpPost("regenerar-codigo")]
        public ActionResult RegenerarCodigo()
        {
            // TODO: Implementar regeneración de código
            // Verificar que el usuario es el creador del grupo

            return Ok(new
            {
                mensaje = "Funcionalidad de regenerar código en desarrollo"
            });
        }
    }

    // DTOs adicionales
    public class InfoGrupoActual
    {
        public bool TieneGrupo { get; set; }
        public int? GrupoId { get; set; }
        public int? UsuarioId { get; set; }
        public string? NombreGrupo { get; set; }
        public string? CodigoInvitacion { get; set; }
        public int? CantidadMiembros { get; set; }
        public bool? EsCreador { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }
}