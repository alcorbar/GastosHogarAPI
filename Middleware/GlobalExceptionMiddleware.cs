using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GastosHogarAPI.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IWebHostEnvironment _environment;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Excepción no manejada: {Message} | Path: {Path} | Method: {Method} | User: {User}",
                    ex.Message,
                    context.Request.Path,
                    context.Request.Method,
                    context.User?.Identity?.Name ?? "Anónimo");

                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Evitar procesar si la respuesta ya ha comenzado
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("No se puede modificar la respuesta porque ya ha comenzado a enviarse");
                return;
            }

            context.Response.ContentType = "application/json";

            var errorResponse = CreateErrorResponse(context, exception);
            context.Response.StatusCode = errorResponse.StatusCode;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _environment.IsDevelopment()
            };

            var jsonResponse = JsonSerializer.Serialize(errorResponse, jsonOptions);
            await context.Response.WriteAsync(jsonResponse);
        }

        private ErrorResponse CreateErrorResponse(HttpContext context, Exception exception)
        {
            var errorResponse = new ErrorResponse
            {
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path.Value ?? string.Empty,
                Method = context.Request.Method,
                TraceId = context.TraceIdentifier
            };

            // Usar if-else en lugar de switch para evitar problemas
            if (exception is ArgumentNullException nullEx)
            {
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Mensaje = "Parámetro requerido faltante";
                errorResponse.Detalle = $"El parámetro '{nullEx.ParamName}' es requerido";
                errorResponse.Codigo = "REQUIRED_PARAMETER";
            }
            else if (exception is ArgumentException argEx)
            {
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Mensaje = "Parámetros inválidos";
                errorResponse.Detalle = argEx.Message;
                errorResponse.Codigo = "INVALID_ARGUMENT";
            }
            else if (exception is UnauthorizedAccessException)
            {
                errorResponse.StatusCode = (int)HttpStatusCode.Unauthorized;
                errorResponse.Mensaje = "Acceso no autorizado";
                errorResponse.Detalle = "No tiene permisos para realizar esta operación";
                errorResponse.Codigo = "UNAUTHORIZED";
            }
            else if (exception is KeyNotFoundException)
            {
                errorResponse.StatusCode = (int)HttpStatusCode.NotFound;
                errorResponse.Mensaje = "Recurso no encontrado";
                errorResponse.Detalle = exception.Message;
                errorResponse.Codigo = "NOT_FOUND";
            }
            else if (exception is InvalidOperationException invOpEx)
            {
                errorResponse.StatusCode = (int)HttpStatusCode.BadRequest;
                errorResponse.Mensaje = "Operación inválida";
                errorResponse.Detalle = invOpEx.Message;
                errorResponse.Codigo = "INVALID_OPERATION";
            }
            else if (exception is TimeoutException)
            {
                errorResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Mensaje = "Tiempo de espera agotado";
                errorResponse.Detalle = "La operación ha excedido el tiempo límite";
                errorResponse.Codigo = "TIMEOUT";
            }
            else if (exception is TaskCanceledException)
            {
                errorResponse.StatusCode = (int)HttpStatusCode.RequestTimeout;
                errorResponse.Mensaje = "Operación cancelada";
                errorResponse.Detalle = "La operación fue cancelada o excedió el tiempo límite";
                errorResponse.Codigo = "OPERATION_CANCELLED";
            }
            else if (exception is NotSupportedException)
            {
                errorResponse.StatusCode = (int)HttpStatusCode.NotImplemented;
                errorResponse.Mensaje = "Operación no soportada";
                errorResponse.Detalle = exception.Message;
                errorResponse.Codigo = "NOT_SUPPORTED";
            }
            else if (exception is DbUpdateException dbEx)
            {
                errorResponse.StatusCode = (int)HttpStatusCode.Conflict;
                errorResponse.Mensaje = "Error de base de datos";
                errorResponse.Codigo = "DATABASE_ERROR";

                if (_environment.IsDevelopment())
                {
                    errorResponse.Detalle = dbEx.Message;
                }
                else
                {
                    var innerMessage = dbEx.InnerException?.Message ?? string.Empty;
                    if (innerMessage.Contains("UNIQUE KEY constraint") ||
                        innerMessage.Contains("duplicate key"))
                    {
                        errorResponse.Detalle = "Ya existe un registro con estos datos";
                        errorResponse.Codigo = "DUPLICATE_ENTRY";
                    }
                    else if (innerMessage.Contains("FOREIGN KEY constraint"))
                    {
                        errorResponse.Detalle = "No se puede completar la operación debido a referencias existentes";
                        errorResponse.Codigo = "FOREIGN_KEY_VIOLATION";
                    }
                    else
                    {
                        errorResponse.Detalle = "Error al procesar los datos";
                    }
                }
            }
            else
            {
                // Caso por defecto
                errorResponse.StatusCode = (int)HttpStatusCode.InternalServerError;
                errorResponse.Mensaje = "Error interno del servidor";
                errorResponse.Codigo = "INTERNAL_ERROR";

                if (_environment.IsDevelopment())
                {
                    errorResponse.Detalle = exception.Message;
                    errorResponse.StackTrace = exception.StackTrace;
                    errorResponse.InnerException = exception.InnerException?.Message;
                }
                else
                {
                    errorResponse.Detalle = "Ha ocurrido un error inesperado. Por favor, intente nuevamente";
                }
            }

            return errorResponse;
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
    }

    // Extensión para registrar el middleware
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}