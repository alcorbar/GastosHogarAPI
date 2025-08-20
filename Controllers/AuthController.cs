using GastosHogarAPI.Models.DTOs;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastosHogarAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Login tradicional con PIN
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            _logger.LogInformation("Intento de login para usuario: {Usuario}", request.Usuario);

            var response = await _authService.LoginAsync(request);

            if (!response.Exito)
            {
                _logger.LogWarning("Login fallido para usuario: {Usuario} - {Mensaje}", request.Usuario, response.Mensaje);
                return BadRequest(response);
            }

            _logger.LogInformation("Login exitoso para usuario: {Usuario} (ID: {UsuarioId})",
                request.Usuario, response.UsuarioId);
            return Ok(response);
        }

        /// <summary>
        /// Login con Google
        /// </summary>
        [HttpPost("google-login")]
        public async Task<ActionResult<LoginResponse>> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            _logger.LogInformation("Intento de login con Google para dispositivo: {DeviceId}", request.DeviceId);

            var response = await _authService.GoogleLoginAsync(request);

            if (!response.Exito)
            {
                _logger.LogWarning("Login con Google fallido: {Mensaje}", response.Mensaje);
                return BadRequest(response);
            }

            _logger.LogInformation("Login con Google exitoso para usuario: {Email} (ID: {UsuarioId})",
                response.Email, response.UsuarioId);
            return Ok(response);
        }

        /// <summary>
        /// Auto-login para dispositivos conocidos
        /// </summary>
        [HttpPost("auto-login")]
        public async Task<ActionResult<LoginResponse>> AutoLogin([FromBody] AutoLoginRequest request)
        {
            _logger.LogInformation("Intento de auto-login para dispositivo: {DeviceId}", request.DeviceId);

            var response = await _authService.AutoLoginAsync(request);

            if (!response.Exito)
            {
                _logger.LogInformation("Auto-login fallido para dispositivo: {DeviceId}", request.DeviceId);
                return BadRequest(response);
            }

            _logger.LogInformation("Auto-login exitoso para usuario: {UsuarioId}", response.UsuarioId);
            return Ok(response);
        }

        /// <summary>
        /// Buscar usuario por email
        /// </summary>
        [HttpGet("buscar-usuario")]
        [Authorize]
        public async Task<ActionResult<UsuarioBusquedaResponse>> BuscarUsuario([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new { mensaje = "Email es requerido" });
            }

            _logger.LogInformation("Búsqueda de usuario por email: {Email}", email);

            var response = await _authService.BuscarUsuarioPorEmailAsync(email);
            return Ok(response);
        }

        /// <summary>
        /// Cambiar PIN de usuario
        /// </summary>
        [HttpPost("cambiar-pin")]
        [Authorize]
        public async Task<ActionResult> CambiarPin([FromBody] CambiarPinRequest request)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var exito = await _authService.CambiarPinAsync(userId, request.NuevoPin);

            if (!exito)
            {
                return BadRequest(new { mensaje = "Error al cambiar PIN" });
            }

            _logger.LogInformation("PIN cambiado exitosamente para usuario: {UsuarioId}", userId);
            return Ok(new { mensaje = "PIN actualizado correctamente" });
        }

        /// <summary>
        /// Verificar estado del token actual
        /// </summary>
        [HttpGet("verify-token")]
        [Authorize]
        public ActionResult VerifyToken()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            var userName = User.FindFirst("UserName")?.Value;
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            return Ok(new
            {
                valido = true,
                usuarioId = userId,
                nombreUsuario = userName,
                grupoId = int.TryParse(grupoIdClaim, out var grupoId) ? grupoId : (int?)null,
                mensaje = "Token válido"
            });
        }
    }

    // DTO adicional para cambiar PIN
    public class CambiarPinRequest
    {
        public string NuevoPin { get; set; } = string.Empty;
    }
}