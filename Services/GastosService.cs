using GastosHogarAPI.Data;
using GastosHogarAPI.Models;
using GastosHogarAPI.Models.DTOs;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace GastosHogarAPI.Services
{
    public class GastosService : IGastosService
    {
        private readonly AppDbContext _context;
        private readonly CacheService _cache;

        public GastosService(AppDbContext context, CacheService cache)
        {
            _context = context;
            _cache = cache;
        }

        // ACTUALIZAR: Crear gasto (ahora con más información)
        public async Task<int> CrearGastoAsync(Gasto gasto)
        {
            gasto.Fecha = DateTime.Now;
            gasto.Mes = gasto.Fecha.Month;
            gasto.Año = gasto.Fecha.Year;
            gasto.FechaCreacion = DateTime.Now;

            _context.Gastos.Add(gasto);
            await _context.SaveChangesAsync();

            // Invalidar cache relacionado
            _cache.Remove($"gastos_{gasto.GrupoId}_{gasto.Mes}_{gasto.Año}");
            _cache.Remove($"resumen_{gasto.GrupoId}_{gasto.Mes}_{gasto.Año}");

            // Actualizar estadísticas del usuario
            await ActualizarEstadisticasUsuarioAsync(gasto.UsuarioId);

            return gasto.Id;
        }

        // ACTUALIZAR: Crear gasto desde DTO
        public async Task<int> CrearGastoAsync(CrearGastoRequest request)
        {
            var gasto = new Gasto
            {
                UsuarioId = request.UsuarioId,
                GrupoId = request.GrupoId,
                Importe = request.Importe,
                CategoriaId = request.CategoriaId,
                Descripcion = request.Descripcion,
                EsDetalle = request.EsDetalle,
                Tienda = request.Tienda,
                Notas = request.Notas,
                FotoTicket = request.FotoTicket,
                NombreFotoTicket = request.NombreFotoTicket,
                Latitud = request.Latitud,
                Longitud = request.Longitud
            };

            return await CrearGastoAsync(gasto);
        }

        // ACTUALIZAR: Obtener gastos del mes CON CACHE
        public async Task<List<object>> ObtenerGastosMesAsync(int mes, int año)
        {
            var cacheKey = $"gastos_todos_{mes}_{año}";

            var result = await _cache.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    return await _context.Gastos
                        .Include(g => g.Usuario)
                        .Include(g => g.Categoria)
                        .Where(g => g.Mes == mes && g.Año == año)
                        .Select(g => new
                        {
                            g.Id,
                            g.UsuarioId,
                            g.Fecha,
                            g.Importe,
                            g.CategoriaId,
                            g.Descripcion,
                            EsDetalle = g.EsDetalle,
                            g.Mes,
                            g.Año,
                            g.Tienda,
                            g.Notas,
                            TieneFoto = g.FotoTicket != null,
                            NombreUsuario = g.Usuario!.Nombre,
                            AliasUsuario = g.Usuario.Alias,
                            ColorUsuario = g.Usuario.ColorPersonalizado,
                            Categoria = g.Categoria!.Nombre,
                            EmojiCategoria = g.Categoria.Emoji,
                            ColorCategoria = g.Categoria.Color,
                            TipoGastoTexto = g.EsDetalle ? "💝 Mi detalle" : "🤝 Compartido"
                        })
                        .OrderByDescending(g => g.Fecha)
                        .Cast<object>() // Convertir explícitamente a object
                        .ToListAsync(); // Ahora sin el tipo genérico
                },
                TimeSpan.FromMinutes(10) // Cache por 10 minutos
            );

              return result ?? new List<object>();

        }

        // NUEVO: Obtener gastos del mes por grupo CON CACHE
        public async Task<List<GastoResponse>> ObtenerGastosMesGrupoAsync(int grupoId, int mes, int año)
        {
            var cacheKey = $"gastos_{grupoId}_{mes}_{año}";

            var result = await _cache.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    return await _context.Gastos
                        .Include(g => g.Usuario)
                        .Include(g => g.Categoria)
                        .Where(g => g.GrupoId == grupoId && g.Mes == mes && g.Año == año)
                        .Select(g => new GastoResponse
                        {
                            Id = g.Id,
                            UsuarioId = g.UsuarioId,
                            NombreUsuario = g.Usuario!.Nombre,
                            AliasUsuario = g.Usuario.Alias,
                            ColorUsuario = g.Usuario.ColorPersonalizado,
                            FotoUsuario = g.Usuario.FotoUrl,
                            Fecha = g.Fecha,
                            Importe = g.Importe,
                            CategoriaId = g.CategoriaId,
                            NombreCategoria = g.Categoria!.Nombre,
                            EmojiCategoria = g.Categoria.Emoji,
                            ColorCategoria = g.Categoria.Color,
                            Descripcion = g.Descripcion,
                            EsDetalle = g.EsDetalle,
                            Tienda = g.Tienda,
                            Notas = g.Notas,
                            TieneFoto = g.FotoTicket != null,
                            FechaCreacion = g.FechaCreacion,
                            FechaModificacion = g.FechaModificacion
                        })
                        .OrderByDescending(g => g.Fecha)
                        .ToListAsync();
                },
                TimeSpan.FromMinutes(10) // Cache por 10 minutos
            );

            return result ?? new List<GastoResponse>();
        }

        // ACTUALIZAR: Resumen mensual (compatible con versión anterior)
        public async Task<ResumenMensual> ObtenerResumenMensualAsync(int mes, int año)
        {
            // Buscar cualquier estado mensual existente para el mes/año
            var estadoExistente = await _context.EstadoMensual
                .FirstOrDefaultAsync(e => e.Mes == mes && e.Año == año);

            if (estadoExistente != null && estadoExistente.GrupoId > 0)
            {
                return await ObtenerResumenMensualGrupoAsync(estadoExistente.GrupoId, mes, año);
            }

            // Buscar el primer grupo activo con gastos en ese mes/año
            var grupoConGastos = await _context.Gastos
                .Where(g => g.Mes == mes && g.Año == año)
                .Select(g => g.GrupoId)
                .FirstOrDefaultAsync();

            if (grupoConGastos > 0)
            {
                return await ObtenerResumenMensualGrupoAsync(grupoConGastos, mes, año);
            }

            return new ResumenMensual
            {
                Mes = mes,
                Año = año,
                GrupoId = 0,
                ExplicacionDetallada = "No hay gastos registrados para este mes."
            };
        }

        // ✅ ACTUALIZADO: Resumen mensual por grupo CON CACHE INTELIGENTE
        public async Task<ResumenMensual> ObtenerResumenMensualGrupoAsync(int grupoId, int mes, int año)
        {
            // Verificar si hay cambios recientes para invalidar cache
            var ultimoCambio = await _context.Gastos
                .Where(g => g.GrupoId == grupoId && g.Mes == mes && g.Año == año)
                .Select(g => (DateTime?)(g.FechaModificacion ?? g.FechaCreacion))
                .OrderByDescending(f => f)
                .FirstOrDefaultAsync();

            var timestamp = ultimoCambio?.Ticks ?? 0;
            var cacheKey = $"resumen_{grupoId}_{mes}_{año}_{timestamp}";

            var result = await _cache.GetOrCreateAsync(
                cacheKey,
                async () =>
                {
                    var gastos = await _context.Gastos
                        .Include(g => g.Usuario)
                        .Where(g => g.GrupoId == grupoId && g.Mes == mes && g.Año == año)
                        .ToListAsync();

                    var usuarios = await _context.Usuarios
                        .Where(u => u.GrupoId == grupoId && u.Activo)
                        .ToListAsync();

                    // Calcular totales por usuario
                    var gastosPorUsuario = new Dictionary<int, decimal>();
                    var detallesPorUsuario = new Dictionary<int, decimal>();
                    var gastosCompartidosPorUsuario = new Dictionary<int, decimal>();

                    foreach (var usuario in usuarios)
                    {
                        var gastosUsuario = gastos.Where(g => g.UsuarioId == usuario.Id);
                        var totalUsuario = gastosUsuario.Sum(g => g.Importe);
                        var detallesUsuario = gastosUsuario.Where(g => g.EsDetalle).Sum(g => g.Importe);
                        var compartidoUsuario = totalUsuario - detallesUsuario;

                        gastosPorUsuario[usuario.Id] = totalUsuario;
                        detallesPorUsuario[usuario.Id] = detallesUsuario;
                        gastosCompartidosPorUsuario[usuario.Id] = compartidoUsuario;
                    }

                    var totalGastos = gastosPorUsuario.Values.Sum();
                    var totalDetalles = detallesPorUsuario.Values.Sum();
                    var totalCompartido = gastosCompartidosPorUsuario.Values.Sum();
                    var cuotaPorPersona = usuarios.Count > 0 ? totalCompartido / usuarios.Count : 0;

                    // ✅ Calcular liquidación multiusuario
                    var (deudorId, acreedorId, montoDeuda) = CalcularLiquidacionMultiusuario(
                        gastosCompartidosPorUsuario, usuarios, cuotaPorPersona);

                    // Obtener estado de confirmación
                    var estado = await _context.EstadoMensual
                        .FirstOrDefaultAsync(e => e.GrupoId == grupoId && e.Mes == mes && e.Año == año);

                    var confirmaciones = estado?.ConfirmacionesUsuarios ?? new Dictionary<int, bool>();

                    // ✅ Información detallada de usuarios
                    var informacionUsuarios = usuarios.ToDictionary(
                        u => u.Id,
                        u => new UsuarioResumen
                        {
                            Id = u.Id,
                            Nombre = u.Nombre,
                            Alias = u.Alias,
                            ColorPersonalizado = u.ColorPersonalizado,
                            FotoUrl = u.FotoUrl,
                            Confirmado = confirmaciones.GetValueOrDefault(u.Id, false),
                            TotalGastado = gastosPorUsuario.GetValueOrDefault(u.Id, 0),
                            DetallesGastados = detallesPorUsuario.GetValueOrDefault(u.Id, 0),
                            CompartidoGastado = gastosCompartidosPorUsuario.GetValueOrDefault(u.Id, 0)
                        });

                    return new ResumenMensual
                    {
                        Mes = mes,
                        Año = año,
                        GrupoId = grupoId,
                        GastosPorUsuario = gastosPorUsuario,
                        DetallesPorUsuario = detallesPorUsuario,
                        GastosCompartidosPorUsuario = gastosCompartidosPorUsuario,
                        InformacionUsuarios = informacionUsuarios,
                        TotalGastos = totalGastos,
                        TotalDetalles = totalDetalles,
                        TotalCompartido = totalCompartido,
                        CuotaPorPersona = cuotaPorPersona,
                        DeudorId = deudorId,
                        AcreedorId = acreedorId,
                        MontoDeuda = montoDeuda,
                        NombreDeudor = deudorId.HasValue ? usuarios.FirstOrDefault(u => u.Id == deudorId)?.Alias ?? usuarios.FirstOrDefault(u => u.Id == deudorId)?.Nombre : null,
                        NombreAcreedor = acreedorId.HasValue ? usuarios.FirstOrDefault(u => u.Id == acreedorId)?.Alias ?? usuarios.FirstOrDefault(u => u.Id == acreedorId)?.Nombre : null,
                        ConfirmacionesUsuarios = confirmaciones,
                        TodosConfirmaron = confirmaciones.Count == usuarios.Count && confirmaciones.Values.All(c => c),
                        LiquidacionCalculada = estado?.LiquidacionCalculada ?? false,
                        EstadoPago = estado?.EstadoPago ?? "pendiente",
                        PlanPagoId = estado?.PlanPagoId,
                        ExplicacionDetallada = GenerarExplicacionDetallada(gastosPorUsuario, detallesPorUsuario, usuarios, cuotaPorPersona, montoDeuda, deudorId, acreedorId)
                    };
                },
                TimeSpan.FromMinutes(15) // Cache por 15 minutos
            );

            // ✅ Garantizar que nunca retornamos null
            return result ?? new ResumenMensual
            {
                Mes = mes,
                Año = año,
                GrupoId = grupoId,
                ExplicacionDetallada = "No se pudieron obtener datos del resumen."
            };
        }

        // ✅ NUEVO: Cálculo de liquidación multiusuario
        private (int? deudorId, int? acreedorId, decimal montoDeuda) CalcularLiquidacionMultiusuario(
            Dictionary<int, decimal> gastosCompartidosPorUsuario,
            List<Usuario> usuarios,
            decimal cuotaPorPersona)
        {
            if (usuarios.Count < 2) return (null, null, 0);

            // Calcular balances (cuánto debe pagar cada uno vs cuánto gastó)
            var balances = usuarios.ToDictionary(
                u => u.Id,
                u => gastosCompartidosPorUsuario.GetValueOrDefault(u.Id, 0) - cuotaPorPersona
            );

            // Encontrar el mayor deudor (balance más negativo) y mayor acreedor (balance más positivo)
            var mayorDeudor = balances.OrderBy(b => b.Value).First();
            var mayorAcreedor = balances.OrderByDescending(b => b.Value).First();

            // Si no hay diferencias significativas (< 0.01€), no hay deuda
            if (Math.Abs(mayorDeudor.Value) < 0.01m || Math.Abs(mayorAcreedor.Value) < 0.01m)
            {
                return (null, null, 0);
            }

            // La deuda es el menor de los dos valores absolutos
            var montoDeuda = Math.Min(Math.Abs(mayorDeudor.Value), Math.Abs(mayorAcreedor.Value));

            return (mayorDeudor.Key, mayorAcreedor.Key, Math.Round(montoDeuda, 2));
        }

        // ✅ ACTUALIZADO: Explicación multiusuario
        private string GenerarExplicacionDetallada(
            Dictionary<int, decimal> gastosPorUsuario,
            Dictionary<int, decimal> detallesPorUsuario,
            List<Usuario> usuarios,
            decimal cuotaPorPersona,
            decimal montoDeuda,
            int? deudorId,
            int? acreedorId)
        {
            var explicacion = new StringBuilder();
            explicacion.AppendLine("💰 EXPLICACIÓN PASO A PASO:");
            explicacion.AppendLine();

            // Paso 1: Gastos totales
            explicacion.AppendLine("📝 PASO 1: ¿Quién gastó qué?");
            foreach (var usuario in usuarios)
            {
                var total = gastosPorUsuario.GetValueOrDefault(usuario.Id);
                var emoji = GetEmojiForUser(usuario);
                var nombre = usuario.Alias ?? usuario.Nombre;
                explicacion.AppendLine($"{emoji} {nombre} gastó: €{total:F2}");
            }
            explicacion.AppendLine($"🏠 Total del grupo: €{gastosPorUsuario.Values.Sum():F2}");
            explicacion.AppendLine();

            // Paso 2: Detalles
            explicacion.AppendLine("💝 PASO 2: ¿Hubo detalles especiales?");
            explicacion.AppendLine("Los \"Mi detalle\" NO se dividen - son regalos entre miembros:");
            foreach (var usuario in usuarios)
            {
                var detalles = detallesPorUsuario.GetValueOrDefault(usuario.Id);
                var emoji = GetEmojiForUser(usuario);
                var nombre = usuario.Alias ?? usuario.Nombre;
                explicacion.AppendLine($"{emoji} {nombre} hizo detalles por: €{detalles:F2}");
            }
            explicacion.AppendLine();

            // Paso 3: Gastos a dividir
            var gastosCompartidos = gastosPorUsuario.Keys.ToDictionary(
                k => k,
                k => gastosPorUsuario[k] - detallesPorUsuario.GetValueOrDefault(k));

            explicacion.AppendLine("🧮 PASO 3: ¿Qué gastos SÍ se dividen?");
            foreach (var usuario in usuarios)
            {
                var compartido = gastosCompartidos[usuario.Id];
                var emoji = GetEmojiForUser(usuario);
                var nombre = usuario.Alias ?? usuario.Nombre;
                explicacion.AppendLine($"{emoji} {nombre} gastó para el grupo: €{compartido:F2}");
            }
            explicacion.AppendLine($"🏠 Total para dividir: €{gastosCompartidos.Values.Sum():F2}");
            explicacion.AppendLine();

            // Paso 4: Cuota por persona
            explicacion.AppendLine("⚖️ PASO 4: ¿Cuánto debe pagar cada uno?");
            explicacion.AppendLine($"€{gastosCompartidos.Values.Sum():F2} ÷ {usuarios.Count} personas = €{cuotaPorPersona:F2} cada uno");
            explicacion.AppendLine();

            // Paso 5: Balance de cada usuario
            explicacion.AppendLine("📊 PASO 5: Balance de cada miembro:");
            foreach (var usuario in usuarios)
            {
                var gastado = gastosCompartidos[usuario.Id];
                var balance = gastado - cuotaPorPersona;
                var emoji = GetEmojiForUser(usuario);
                var nombre = usuario.Alias ?? usuario.Nombre;

                if (balance > 0.01m)
                    explicacion.AppendLine($"{emoji} {nombre}: Gastó €{gastado:F2} - debe recibir €{balance:F2}");
                else if (balance < -0.01m)
                    explicacion.AppendLine($"{emoji} {nombre}: Gastó €{gastado:F2} - debe pagar €{Math.Abs(balance):F2}");
                else
                    explicacion.AppendLine($"{emoji} {nombre}: Gastó €{gastado:F2} - está a la par ✅");
            }
            explicacion.AppendLine();

            // Resultado final
            if (montoDeuda > 0 && deudorId.HasValue && acreedorId.HasValue)
            {
                var deudor = usuarios.FirstOrDefault(u => u.Id == deudorId);
                var acreedor = usuarios.FirstOrDefault(u => u.Id == acreedorId);

                explicacion.AppendLine("✅ RESULTADO FINAL:");
                explicacion.AppendLine($"💸 {deudor?.Alias ?? deudor?.Nombre} debe €{montoDeuda:F2} a {acreedor?.Alias ?? acreedor?.Nombre}");
                explicacion.AppendLine();
                explicacion.AppendLine("💡 TIP: Esta es la transferencia principal. Pueden hacer ajustes menores directamente.");
            }
            else
            {
                explicacion.AppendLine("✅ RESULTADO FINAL:");
                explicacion.AppendLine("🎉 ¡El grupo está a la par! No hay deudas pendientes.");
            }

            return explicacion.ToString();
        }

        // ✅ Emoji dinámico basado en el usuario
        private string GetEmojiForUser(Usuario usuario)
        {
            // Combinación de color + posición en lista para más variedad
            var baseEmoji = usuario.ColorPersonalizado switch
            {
                "#E91E63" => "👩", // Rosa - mujer
                "#2196F3" => "👨", // Azul - hombre  
                "#4CAF50" => "🧑", // Verde - persona
                "#FF9800" => "👤", // Naranja - silueta
                "#9C27B0" => "🙋", // Púrpura - persona levantando mano
                "#FF5722" => "🤵", // Rojo - persona formal
                "#607D8B" => "👥", // Gris - grupo
                _ => "👤"
            };

            return baseEmoji;
        }

        private async Task ActualizarEstadisticasUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario != null)
            {
                var stats = await _context.Gastos
                    .Where(g => g.UsuarioId == usuarioId)
                    .GroupBy(g => g.UsuarioId)
                    .Select(g => new
                    {
                        TotalGastado = g.Sum(x => x.Importe),
                        GastosCreados = g.Count()
                    })
                    .FirstOrDefaultAsync();

                if (stats != null)
                {
                    usuario.TotalGastado = stats.TotalGastado;
                    usuario.GastosCreados = stats.GastosCreados;
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}