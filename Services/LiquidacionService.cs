using GastosHogarAPI.Data;
using GastosHogarAPI.Models;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GastosHogarAPI.Services
{
    public class LiquidacionService : ILiquidacionService
    {
        private readonly AppDbContext _context;
        private readonly IGastosService _gastosService;
        private readonly ILogger<LiquidacionService> _logger;

        public LiquidacionService(AppDbContext context, IGastosService gastosService, ILogger<LiquidacionService> logger)
        {
            _context = context;
            _gastosService = gastosService;
            _logger = logger;
        }

        // ✅ PRINCIPAL: Confirmar gastos por usuario
        public async Task<bool> ConfirmarGastosAsync(int usuarioId, int mes, int año)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null || usuario.GrupoId == null)
            {
                _logger.LogWarning("Usuario {UsuarioId} no encontrado o sin grupo", usuarioId);
                return false;
            }

            var estado = await _context.EstadoMensual
                .FirstOrDefaultAsync(e => e.GrupoId == usuario.GrupoId && e.Mes == mes && e.Año == año);

            if (estado == null)
            {
                estado = new EstadoMensual
                {
                    GrupoId = usuario.GrupoId.Value,
                    Mes = mes,
                    Año = año
                };
                _context.EstadoMensual.Add(estado);
                _logger.LogInformation("Creado nuevo EstadoMensual para grupo {GrupoId}, {Mes}/{Año}",
                    usuario.GrupoId, mes, año);
            }

            // Marcar confirmación del usuario
            estado.ConfirmacionesUsuarios[usuarioId] = true;

            // Obtener todos los miembros activos del grupo
            var miembrosGrupo = await _context.Usuarios
                .Where(u => u.GrupoId == usuario.GrupoId && u.Activo)
                .ToListAsync();

            // Verificar si todos confirmaron
            var todosConfirmaron = miembrosGrupo.All(m =>
                estado.ConfirmacionesUsuarios.ContainsKey(m.Id) &&
                estado.ConfirmacionesUsuarios[m.Id]);

            estado.TodosConfirmaron = todosConfirmaron;

            // Si todos confirmaron, calcular liquidación
            if (todosConfirmaron && !estado.LiquidacionCalculada)
            {
                _logger.LogInformation("Todos los usuarios confirmaron. Calculando liquidación para grupo {GrupoId}, {Mes}/{Año}",
                    usuario.GrupoId, mes, año);

                var resumen = await _gastosService.ObtenerResumenMensualGrupoAsync(usuario.GrupoId.Value, mes, año);
                estado.MontoDeuda = resumen.MontoDeuda;
                estado.DeudorId = resumen.DeudorId;
                estado.AcreedorId = resumen.AcreedorId;
                estado.LiquidacionCalculada = true;
                estado.FechaLiquidacion = DateTime.Now;

                _logger.LogInformation("Liquidación calculada: {NombreDeudor} debe €{MontoDeuda} a {NombreAcreedor}",
                    resumen.NombreDeudor, resumen.MontoDeuda, resumen.NombreAcreedor);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // ✅ Obtener estado por grupo - SIN INCLUDES problemáticos
        public async Task<EstadoMensual?> ObtenerEstadoMensualAsync(int grupoId, int mes, int año)
        {
            return await _context.EstadoMensual
                .FirstOrDefaultAsync(e => e.GrupoId == grupoId && e.Mes == mes && e.Año == año);
        }

        // ✅ Marcar pago realizado
        public async Task<bool> MarcarPagoRealizadoAsync(int grupoId, int mes, int año, string metodoPago)
        {
            var estado = await _context.EstadoMensual
                .FirstOrDefaultAsync(e => e.GrupoId == grupoId && e.Mes == mes && e.Año == año);

            if (estado == null || !estado.LiquidacionCalculada)
            {
                _logger.LogWarning("No se puede marcar pago: estado no encontrado o liquidación no calculada");
                return false;
            }

            estado.EstadoPago = "pagado";
            estado.FechaPago = DateTime.Now;
            estado.MetodoPago = metodoPago;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Pago marcado como realizado para grupo {GrupoId}, {Mes}/{Año} via {MetodoPago}",
                grupoId, mes, año, metodoPago);

            return true;
        }

        // ✅ Confirmar pago recibido
        public async Task<bool> ConfirmarPagoRecibidoAsync(int grupoId, int mes, int año)
        {
            var estado = await _context.EstadoMensual
                .FirstOrDefaultAsync(e => e.GrupoId == grupoId && e.Mes == mes && e.Año == año);

            if (estado == null || estado.EstadoPago != "pagado")
            {
                _logger.LogWarning("No se puede confirmar pago: estado incorrecto");
                return false;
            }

            estado.EstadoPago = "confirmado";
            await _context.SaveChangesAsync();

            _logger.LogInformation("Pago confirmado para grupo {GrupoId}, {Mes}/{Año}", grupoId, mes, año);

            return true;
        }

        // ✅ Obtener estado por usuario (deduce el grupo)
        public async Task<EstadoMensual?> ObtenerEstadoMensualPorUsuarioAsync(int usuarioId, int mes, int año)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario?.GrupoId == null) return null;

            return await ObtenerEstadoMensualAsync(usuario.GrupoId.Value, mes, año);
        }

        // ✅ Verificar si usuario puede confirmar
        public async Task<bool> PuedeConfirmarAsync(int usuarioId, int mes, int año)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario?.GrupoId == null) return false;

            var estado = await ObtenerEstadoMensualAsync(usuario.GrupoId.Value, mes, año);

            // Puede confirmar si no lo ha hecho ya y el estado no está cerrado
            var yaConfirmo = estado?.ConfirmacionesUsuarios.GetValueOrDefault(usuarioId, false) == true;
            var liquidacionCerrada = estado?.EstadoPago == "confirmado";

            return !yaConfirmo && !liquidacionCerrada;
        }

        // ✅ Obtener usuarios pendientes de confirmación
        public async Task<List<int>> ObtenerUsuariosPendientesConfirmacionAsync(int grupoId, int mes, int año)
        {
            var estado = await ObtenerEstadoMensualAsync(grupoId, mes, año);
            if (estado == null) return new List<int>();

            var miembrosGrupo = await _context.Usuarios
                .Where(u => u.GrupoId == grupoId && u.Activo)
                .Select(u => u.Id)
                .ToListAsync();

            return miembrosGrupo
                .Where(userId => !estado.ConfirmacionesUsuarios.GetValueOrDefault(userId, false))
                .ToList();
        }

        // ✅ Contar confirmaciones actuales
        public async Task<int> ContarConfirmacionesAsync(int grupoId, int mes, int año)
        {
            var estado = await ObtenerEstadoMensualAsync(grupoId, mes, año);
            return estado?.ConfirmacionesUsuarios.Count(c => c.Value) ?? 0;
        }
    }
}