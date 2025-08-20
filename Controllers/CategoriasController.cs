using GastosHogarAPI.Data;
using GastosHogarAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GastosHogarAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CategoriasController> _logger;

        public CategoriasController(AppDbContext context, ILogger<CategoriasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todas las categorías disponibles para el usuario
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<CategoriaResponse>>> ObtenerCategorias()
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            int.TryParse(grupoIdClaim, out var grupoId);

            var categorias = await _context.Categorias
                .Where(c => c.Activa && (c.EsPredeterminada || c.GrupoId == grupoId))
                .OrderBy(c => c.EsPredeterminada ? 0 : 1) // Predeterminadas primero
                .ThenBy(c => c.Nombre)
                .Select(c => new CategoriaResponse
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Emoji = c.Emoji,
                    Color = c.Color,
                    EsPredeterminada = c.EsPredeterminada,
                    VecesUsada = c.VecesUsada,
                    TotalGastado = c.TotalGastado
                })
                .ToListAsync();

            return Ok(categorias);
        }

        /// <summary>
        /// Obtener categorías más usadas por el grupo
        /// </summary>
        [HttpGet("populares")]
        public async Task<ActionResult<List<CategoriaResponse>>> ObtenerCategoriasPopulares([FromQuery] int limite = 10)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var categorias = await _context.Categorias
                .Where(c => c.Activa && (c.EsPredeterminada || c.GrupoId == grupoId))
                .OrderByDescending(c => c.VecesUsada)
                .ThenByDescending(c => c.TotalGastado)
                .Take(limite)
                .Select(c => new CategoriaResponse
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Emoji = c.Emoji,
                    Color = c.Color,
                    EsPredeterminada = c.EsPredeterminada,
                    VecesUsada = c.VecesUsada,
                    TotalGastado = c.TotalGastado
                })
                .ToListAsync();

            return Ok(categorias);
        }

        /// <summary>
        /// Crear una nueva categoría personalizada para el grupo
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<CategoriaResponse>> CrearCategoria([FromBody] CrearCategoriaRequest request)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            // Verificar que no existe una categoría con el mismo nombre en el grupo
            var existeCategoria = await _context.Categorias
                .AnyAsync(c => c.Nombre.ToLower() == request.Nombre.ToLower() &&
                          (c.EsPredeterminada || c.GrupoId == grupoId) && c.Activa);

            if (existeCategoria)
            {
                return BadRequest(new { mensaje = "Ya existe una categoría con ese nombre" });
            }

            var categoria = new Categoria
            {
                Nombre = request.Nombre,
                Emoji = string.IsNullOrEmpty(request.Emoji) ? "📝" : request.Emoji,
                Color = string.IsNullOrEmpty(request.Color) ? "#757575" : request.Color,
                GrupoId = grupoId,
                EsPredeterminada = false,
                Activa = true,
                FechaCreacion = DateTime.Now
            };

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Categoría '{Nombre}' creada para grupo {GrupoId}", categoria.Nombre, grupoId);

            return Ok(new CategoriaResponse
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre,
                Emoji = categoria.Emoji,
                Color = categoria.Color,
                EsPredeterminada = categoria.EsPredeterminada,
                VecesUsada = categoria.VecesUsada,
                TotalGastado = categoria.TotalGastado
            });
        }

        /// <summary>
        /// Actualizar una categoría personalizada del grupo
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<CategoriaResponse>> ActualizarCategoria(int id, [FromBody] ActualizarCategoriaRequest request)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id && c.GrupoId == grupoId && !c.EsPredeterminada);

            if (categoria == null)
            {
                return NotFound("Categoría no encontrada o no se puede modificar");
            }

            // Verificar que no existe otra categoría con el mismo nombre
            var existeOtraCategoria = await _context.Categorias
                .AnyAsync(c => c.Id != id && c.Nombre.ToLower() == request.Nombre.ToLower() &&
                          (c.EsPredeterminada || c.GrupoId == grupoId) && c.Activa);

            if (existeOtraCategoria)
            {
                return BadRequest(new { mensaje = "Ya existe otra categoría con ese nombre" });
            }

            categoria.Nombre = request.Nombre;
            categoria.Emoji = request.Emoji;
            categoria.Color = request.Color;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Categoría {Id} actualizada para grupo {GrupoId}", id, grupoId);

            return Ok(new CategoriaResponse
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre,
                Emoji = categoria.Emoji,
                Color = categoria.Color,
                EsPredeterminada = categoria.EsPredeterminada,
                VecesUsada = categoria.VecesUsada,
                TotalGastado = categoria.TotalGastado
            });
        }

        /// <summary>
        /// Desactivar una categoría personalizada del grupo
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DesactivarCategoria(int id)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id && c.GrupoId == grupoId && !c.EsPredeterminada);

            if (categoria == null)
            {
                return NotFound("Categoría no encontrada o no se puede eliminar");
            }

            // Verificar si la categoría está siendo usada
            var enUso = await _context.Gastos.AnyAsync(g => g.CategoriaId == id);

            if (enUso)
            {
                categoria.Activa = false; // Solo desactivar si está en uso
                _logger.LogInformation("Categoría {Id} desactivada para grupo {GrupoId}", id, grupoId);
            }
            else
            {
                _context.Categorias.Remove(categoria); // Eliminar si no está en uso
                _logger.LogInformation("Categoría {Id} eliminada para grupo {GrupoId}", id, grupoId);
            }

            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Categoría eliminada exitosamente" });
        }

        /// <summary>
        /// Obtener estadísticas de uso de categorías
        /// </summary>
        [HttpGet("estadisticas")]
        public async Task<ActionResult<List<EstadisticaCategoriaResponse>>> ObtenerEstadisticas([FromQuery] int? mes = null, [FromQuery] int? año = null)
        {
            var grupoIdClaim = User.FindFirst("GrupoId")?.Value;
            if (!int.TryParse(grupoIdClaim, out var grupoId))
            {
                return BadRequest("Usuario no pertenece a ningún grupo");
            }

            var query = _context.Gastos
                .Include(g => g.Categoria)
                .Where(g => g.GrupoId == grupoId);

            if (mes.HasValue && año.HasValue)
            {
                query = query.Where(g => g.Mes == mes && g.Año == año);
            }

            var estadisticas = await query
                .GroupBy(g => new { g.CategoriaId, g.Categoria!.Nombre, g.Categoria.Emoji, g.Categoria.Color })
                .Select(g => new EstadisticaCategoriaResponse
                {
                    CategoriaId = g.Key.CategoriaId,
                    Nombre = g.Key.Nombre,
                    Emoji = g.Key.Emoji,
                    Color = g.Key.Color,
                    TotalGastos = g.Count(),
                    TotalImporte = g.Sum(x => x.Importe),
                    ImportePromedio = g.Average(x => x.Importe),
                    ImporteMaximo = g.Max(x => x.Importe),
                    Porcentaje = 0 // Se calculará después
                })
                .OrderByDescending(e => e.TotalImporte)
                .ToListAsync();

            // Calcular porcentajes
            var totalGeneral = estadisticas.Sum(e => e.TotalImporte);
            if (totalGeneral > 0)
            {
                foreach (var estadistica in estadisticas)
                {
                    estadistica.Porcentaje = Math.Round((estadistica.TotalImporte / totalGeneral) * 100, 2);
                }
            }

            return Ok(estadisticas);
        }
    }

    // DTOs para el controlador
    public class CategoriaResponse
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool EsPredeterminada { get; set; }
        public int VecesUsada { get; set; }
        public decimal TotalGastado { get; set; }
    }

    public class CrearCategoriaRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class ActualizarCategoriaRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class EstadisticaCategoriaResponse
    {
        public int CategoriaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Emoji { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public int TotalGastos { get; set; }
        public decimal TotalImporte { get; set; }
        public decimal ImportePromedio { get; set; }
        public decimal ImporteMaximo { get; set; }
        public decimal Porcentaje { get; set; }
    }
}