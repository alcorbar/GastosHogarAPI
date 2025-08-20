using GastosHogarAPI.Exceptions;
using System.Net;
using System.Text.Json;

namespace GastosHogarAPI.Middleware
{
    public class BusinessExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<BusinessExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public BusinessExceptionMiddleware(
            RequestDelegate next,
            ILogger<BusinessExceptionMiddleware> logger,
            IWebHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BusinessException ex)
            {
                _logger.LogWarning("Excepción de negocio: {ErrorCode} - {Message} | Path: {Path} | User: {User}",
                    ex.ErrorCode,
                    ex.Message,
                    context.Request.Path,
                    context.User?.Identity?.Name ?? "Anónimo");

                await HandleBusinessExceptionAsync(context, ex);
            }
        }

        private async Task HandleBusinessExceptionAsync(HttpContext context, BusinessException exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("No se puede modificar la respuesta de excepción de negocio porque ya ha comenzado a enviarse");
                return;
            }

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = exception.StatusCode;

            var response = new BusinessErrorResponse
            {
                StatusCode = exception.StatusCode,
                ErrorCode = exception.ErrorCode,
                Mensaje = exception.Message,
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path.Value ?? string.Empty,
                Method = context.Request.Method,
                TraceId = context.TraceIdentifier
            };

            // Agregar detalles adicionales en desarrollo
            if (_environment.IsDevelopment())
            {
                response.StackTrace = exception.StackTrace;
                response.InnerException = exception.InnerException?.Message;
            }

            // Personalizar mensajes según el tipo de excepción
            response = EnrichErrorResponse(response, exception);

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };

            var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
            await context.Response.WriteAsync(jsonResponse);
        }

        private static BusinessErrorResponse EnrichErrorResponse(BusinessErrorResponse response, BusinessException exception)
        {
            // Personalizar respuestas según el tipo de excepción
            switch (exception)
            {
                case BadRequestException badRequest:
                    response.UserFriendlyMessage = "Los datos proporcionados no son válidos. Por favor, verifica la información e intenta nuevamente.";
                    response.Suggestions = new[]
                    {
                        "Revisa que todos los campos requeridos estén completos",
                        "Verifica que los valores numéricos sean correctos",
                        "Asegúrate de que las fechas tengan el formato correcto"
                    };
                    break;

                case UnauthorizedException unauthorized:
                    response.UserFriendlyMessage = "No tienes permisos para realizar esta acción.";
                    response.Suggestions = new[]
                    {
                        "Verifica que hayas iniciado sesión correctamente",
                        "Asegúrate de tener los permisos necesarios",
                        "Intenta cerrar sesión e iniciar sesión nuevamente"
                    };
                    break;

                case ForbiddenException forbidden:
                    response.UserFriendlyMessage = "Acceso denegado a este recurso.";
                    response.Suggestions = new[]
                    {
                        "Verifica que perteneces al grupo correcto",
                        "Consulta con el administrador del grupo",
                        "Asegúrate de estar accediendo al recurso correcto"
                    };
                    break;

                case NotFoundException notFound:
                    response.UserFriendlyMessage = "El recurso solicitado no fue encontrado.";
                    response.Suggestions = new[]
                    {
                        "Verifica que el ID o código sea correcto",
                        "El recurso puede haber sido eliminado",
                        "Intenta actualizar la página o buscar nuevamente"
                    };
                    break;

                case ConflictException conflict:
                    response.UserFriendlyMessage = "Ya existe un recurso con esos datos o hay un conflicto.";
                    response.Suggestions = new[]
                    {
                        "Verifica que no estés duplicando información",
                        "Revisa si ya existe un registro similar",
                        "Intenta con valores diferentes"
                    };
                    break;

                default:
                    response.UserFriendlyMessage = "Ocurrió un error al procesar tu solicitud.";
                    response.Suggestions = new[]
                    {
                        "Intenta nuevamente en unos momentos",
                        "Verifica tu conexión a internet",
                        "Si el problema persiste, contacta al soporte"
                    };
                    break;
            }

            return response;
        }
    }

    public class BusinessErrorResponse
    {
        public int StatusCode { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string UserFriendlyMessage { get; set; } = string.Empty;
        public string[] Suggestions { get; set; } = Array.Empty<string>();
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
    }

    // Extensión para registrar el middleware
    public static class BusinessExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseBusinessExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BusinessExceptionMiddleware>();
        }
    }
}