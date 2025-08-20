using FluentValidation;
using FluentValidation.AspNetCore;
using GastosHogarAPI.Data;
using GastosHogarAPI.Middleware;
using GastosHogarAPI.Models;
using GastosHogarAPI.Services;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURACIÓN DE SERVICIOS =====

// 1. Configuración de Base de Datos
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("GastosHogarAPI");
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    });

    // Configuración adicional para desarrollo
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

// 2. Configuración de Controladores y APIs
builder.Services.AddControllers(options =>
{
    // Agregar filtro de validación global
    options.Filters.Add<ModelStateValidationFilter>();

    // Configurar comportamiento de API
    options.SuppressAsyncSuffixInActionNames = false;
    options.RespectBrowserAcceptHeader = true;
})
.AddJsonOptions(options =>
{
    // Configuración de JSON
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
});

// 3. Configuración de validación de modelos
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Deshabilitar respuesta automática de validación para que nuestro middleware la maneje
    options.SuppressModelStateInvalidFilter = true;
});

// 4. Registrar servicios personalizados - ¡ESTO FALTABA!
builder.Services.AddScoped<ModelStateValidationFilter>();

// ✅ SERVICIOS PRINCIPALES - INYECCIÓN DE DEPENDENCIAS
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<CacheService>();
builder.Services.AddScoped<BusinessValidationService>();

// ✅ SERVICIOS DE NEGOCIO
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGastosService, GastosService>();
builder.Services.AddScoped<IGrupoService, GrupoService>();
builder.Services.AddScoped<ILiquidacionService, LiquidacionService>();
builder.Services.AddScoped<IPlanPagoService, PlanPagoService>();

// 5. Configuración de FluentValidation
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<Program>();

// 6. Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
            ?? new[] { "http://localhost:3000", "http://localhost:5173" }; // Valores por defecto para desarrollo

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });

    // Política más permisiva para desarrollo
    if (builder.Environment.IsDevelopment())
    {
        options.AddPolicy("DevelopmentCors", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    }
});

// 7. Configuración de autenticación JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings.GetValue<string>("SecretKey") ??
    throw new InvalidOperationException("JWT SecretKey no está configurada");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
        ValidAudience = jwtSettings.GetValue<string>("Audience"),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.FromMinutes(5)
    };

    // Configuración de eventos para logging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("Autenticación JWT falló: {Error}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Desafío JWT iniciado para: {Path}", context.Request.Path);
            return Task.CompletedTask;
        }
    };
});

// 8. Configuración de autorización
builder.Services.AddAuthorization(options =>
{
    // Políticas personalizadas si las necesitas
    options.AddPolicy("RequireValidUser", policy =>
    {
        policy.RequireAuthenticatedUser();
    });

    options.AddPolicy("RequireGroup", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("GrupoId");
    });
});

// 9. Configuración de Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Gastos Hogar API",
        Description = "API para gestión de gastos compartidos del hogar con funcionalidades avanzadas de liquidación",
        Contact = new OpenApiContact
        {
            Name = "Equipo de Desarrollo",
            Email = "dev@gastoshogar.com"
        }
    });

    // Configuración de seguridad JWT en Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT en el formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Incluir comentarios XML si existen
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 10. Configuración de Health Checks con más detalles
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database", tags: new[] { "db" })
    .AddCheck("memory", () =>
    {
        var allocated = GC.GetTotalMemory(false);
        const long threshold = 1024L * 1024L * 1024L; // 1GB

        return allocated < threshold
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy($"Memory usage: {allocated / 1024 / 1024} MB")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"Memory usage is high: {allocated / 1024 / 1024} MB");
    })
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"));

// 11. Configuración de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

if (builder.Environment.IsDevelopment())
{
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
}
else
{
    builder.Logging.SetMinimumLevel(LogLevel.Information);
}

// 12. Configuración de cache (si lo necesitas)
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Límite de elementos en cache
});

// 13. Configuración de compresión de respuestas
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// ===== CONSTRUCCIÓN DE LA APLICACIÓN =====

var app = builder.Build();

// ===== CONFIGURACIÓN DEL PIPELINE DE MIDDLEWARE =====

// 1. Manejo global de excepciones (debe ir primero)
app.UseGlobalExceptionMiddleware();

// 2. Manejo de excepciones de negocio (después del global)
app.UseBusinessExceptionMiddleware();

// 3. Compresión de respuestas
app.UseResponseCompression();

// 3. HTTPS Redirection (solo en producción)
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// 4. Configuración específica para desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Gastos Hogar API v1");
        options.RoutePrefix = string.Empty; // Swagger en la raíz
        options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        options.DefaultModelsExpandDepth(-1); // Ocultar modelos por defecto
    });

    // Usar CORS más permisivo en desarrollo
    app.UseCors("DevelopmentCors");
}
else
{
    // CORS restrictivo en producción
    app.UseCors("AllowedOrigins");
}

// 5. Archivos estáticos (si tienes)
app.UseStaticFiles();

// 6. Routing
app.UseRouting();

// 7. Autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// 8. Middleware de validación personalizado
app.UseValidationMiddleware();

// 9. Health checks
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            duration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
});

// Health check específico para base de datos
app.MapHealthChecks("/health/db", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db"),
    ResponseWriter = async (context, report) =>
    {
        var result = report.Entries.First().Value;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = result.Status.ToString(),
            description = result.Description,
            duration = result.Duration.TotalMilliseconds
        }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
});

// 10. Mapear controladores
app.MapControllers();

// 11. Endpoint de información básica
app.MapGet("/", () => new
{
    Application = "Gastos Hogar API",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Features = new
    {
        Authentication = "JWT Bearer",
        Database = "SQL Server",
        Cache = "In-Memory",
        Validation = "FluentValidation",
        Documentation = "/swagger"
    }
})
.WithOpenApi()
.WithTags("Information");

// ===== INICIALIZACIÓN DE BASE DE DATOS =====

// Aplicar migraciones automáticamente en desarrollo
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await context.Database.MigrateAsync();
        app.Logger.LogInformation("✅ Migraciones aplicadas exitosamente");

        // Seed de datos si es necesario
        await SeedDataAsync(context, app.Logger);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "❌ Error al aplicar migraciones");
    }
}

// ===== LOGGING DE INICIO =====

app.Logger.LogInformation("🚀 Gastos Hogar API iniciada");
app.Logger.LogInformation("🌍 Entorno: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("📊 Swagger disponible en: {SwaggerUrl}",
    app.Environment.IsDevelopment() ? app.Urls.FirstOrDefault() : "N/A");
app.Logger.LogInformation("🏥 Health checks en: /health");

// ===== EJECUTAR LA APLICACIÓN =====

await app.RunAsync();

// ===== MÉTODO AUXILIAR PARA SEED DE DATOS =====

static async Task SeedDataAsync(AppDbContext context, ILogger logger)
{
    try
    {
        // Verificar si ya hay datos
        if (await context.Usuarios.AnyAsync())
        {
            logger.LogInformation("🌱 Base de datos ya contiene datos, omitiendo seed");
            return;
        }

        // Crear usuario de prueba
        var usuarioPrueba = new Usuario
        {
            Nombre = "Usuario Demo",
            Email = "demo@gastoshogar.com",
            Pin = BCrypt.Net.BCrypt.HashPassword("1234"),
            ColorPersonalizado = "#2196F3",
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            UltimoAcceso = DateTime.UtcNow
        };

        context.Usuarios.Add(usuarioPrueba);
        await context.SaveChangesAsync();

        logger.LogInformation("🌱 Datos de prueba creados exitosamente");
        logger.LogInformation("👤 Usuario demo: demo@gastoshogar.com / PIN: 1234");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error al crear datos de prueba");
    }
}