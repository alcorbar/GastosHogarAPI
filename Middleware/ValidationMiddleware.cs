using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace GastosHogarAPI.Middleware
{
    public class ValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ValidationMiddleware> _logger;

        public ValidationMiddleware(RequestDelegate next, ILogger<ValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);

            // Procesar errores de validación si existen
            if (context.Response.StatusCode == (int)HttpStatusCode.BadRequest &&
                context.Items.ContainsKey("ValidationErrors"))
            {
                await HandleValidationErrorsAsync(context);
            }
        }

        private async Task HandleValidationErrorsAsync(HttpContext context)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("No se puede modificar la respuesta de validación porque ya ha comenzado a enviarse");
                return;
            }

            var validationErrors = context.Items["ValidationErrors"] as Dictionary<string, List<string>>;

            if (validationErrors != null && validationErrors.Any())
            {
                var response = new ValidationErrorResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Codigo = "VALIDATION_ERROR",
                    Mensaje = "Errores de validación en los datos enviados",
                    Errores = validationErrors,
                    Timestamp = DateTime.UtcNow,
                    Path = context.Request.Path.Value ?? string.Empty,
                    Method = context.Request.Method,
                    TraceId = context.TraceIdentifier
                };

                context.Response.ContentType = "application/json";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var jsonResponse = JsonSerializer.Serialize(response, jsonOptions);
                await context.Response.WriteAsync(jsonResponse);
            }
        }
    }

    public class ValidationErrorResponse
    {
        public int StatusCode { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public Dictionary<string, List<string>> Errores { get; set; } = new();
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public int TotalErrores => Errores.Values.Sum(v => v.Count);
        public string[] CamposConErrores => Errores.Keys.ToArray();
    }

    // Extensión para registrar el middleware
    public static class ValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseValidationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ValidationMiddleware>();
        }
    }

    // Filtro mejorado para capturar errores de ModelState
    public class ModelStateValidationFilter : IActionFilter
    {
        private readonly ILogger<ModelStateValidationFilter> _logger;

        public ModelStateValidationFilter(ILogger<ModelStateValidationFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = ExtractValidationErrors(context.ModelState);

                _logger.LogInformation("Errores de validación detectados en {Action}: {Errors}",
                    context.ActionDescriptor.DisplayName,
                    string.Join(", ", errors.Keys));

                // Almacenar errores en el contexto para el middleware
                context.HttpContext.Items["ValidationErrors"] = errors;

                // Retornar BadRequest para que el middleware lo maneje
                context.Result = new BadRequestResult();
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // Verificar si hay errores en el resultado de la acción
            if (context.Result is BadRequestObjectResult badRequestResult)
            {
                if (badRequestResult.Value is ValidationProblemDetails validationProblem)
                {
                    var errors = validationProblem.Errors.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.ToList()
                    );

                    context.HttpContext.Items["ValidationErrors"] = errors;
                }
                else if (badRequestResult.Value is SerializableError serializableError)
                {
                    var errors = new Dictionary<string, List<string>>();
                    foreach (var error in serializableError)
                    {
                        if (error.Value is string[] stringArray)
                        {
                            errors[error.Key] = stringArray.ToList();
                        }
                        else if (error.Value is string singleError)
                        {
                            errors[error.Key] = new List<string> { singleError };
                        }
                    }
                    context.HttpContext.Items["ValidationErrors"] = errors;
                }
            }
        }

        private static Dictionary<string, List<string>> ExtractValidationErrors(ModelStateDictionary modelState)
        {
            var errors = new Dictionary<string, List<string>>();

            foreach (var kvp in modelState)
            {
                var key = kvp.Key;
                var modelStateEntry = kvp.Value;

                if (modelStateEntry != null && modelStateEntry.Errors.Count > 0)
                {
                    var errorMessages = new List<string>();

                    foreach (var error in modelStateEntry.Errors)
                    {
                        var errorMessage = !string.IsNullOrEmpty(error.ErrorMessage)
                            ? error.ErrorMessage
                            : "Valor inválido";

                        // Mejorar mensajes de error comunes
                        errorMessage = ImproveErrorMessage(key, errorMessage);
                        errorMessages.Add(errorMessage);
                    }

                    errors[NormalizeFieldName(key)] = errorMessages;
                }
            }

            return errors;
        }

        private static string ImproveErrorMessage(string fieldName, string originalMessage)
        {
            // Mejorar mensajes de error comunes
            if (originalMessage.Contains("The field") && originalMessage.Contains("is required"))
            {
                return $"El campo '{fieldName}' es obligatorio";
            }

            if (originalMessage.Contains("The value") && originalMessage.Contains("is not valid"))
            {
                return $"El valor proporcionado para '{fieldName}' no es válido";
            }

            if (originalMessage.Contains("The field") && originalMessage.Contains("must be a number"))
            {
                return $"El campo '{fieldName}' debe ser un número";
            }

            if (originalMessage.Contains("The field") && originalMessage.Contains("must be between"))
            {
                return originalMessage.Replace("The field", $"El campo '{fieldName}'");
            }

            return originalMessage;
        }

        private static string NormalizeFieldName(string fieldName)
        {
            // Convertir nombres de campos a formato más amigable
            // Por ejemplo: "Usuario.Email" -> "email"
            if (fieldName.Contains('.'))
            {
                fieldName = fieldName.Split('.').Last();
            }

            // Convertir a camelCase
            return char.ToLowerInvariant(fieldName[0]) + fieldName[1..];
        }
    }

    // Atributo para validación personalizada
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = new Dictionary<string, List<string>>();

                foreach (var modelStateKey in context.ModelState.Keys)
                {
                    var modelStateEntry = context.ModelState[modelStateKey];
                    if (modelStateEntry != null && modelStateEntry.Errors.Count > 0)
                    {
                        errors[modelStateKey] = modelStateEntry.Errors
                            .Select(e => e.ErrorMessage)
                            .ToList();
                    }
                }

                context.HttpContext.Items["ValidationErrors"] = errors;
                context.Result = new BadRequestResult();
            }
        }
    }
}