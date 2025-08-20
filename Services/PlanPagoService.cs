using GastosHogarAPI.Data;
using GastosHogarAPI.Models;
using GastosHogarAPI.Models.DTOs;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GastosHogarAPI.Services
{
    public class PlanPagoService : IPlanPagoService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<PlanPagoService> _logger;

        public PlanPagoService(AppDbContext context, ILogger<PlanPagoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> CrearPlanPagoAsync(CrearPlanPagoRequest request)
        {
            _logger.LogInformation("Creando plan de pago: Deudor {DeudorId} → Acreedor {AcreedorId}, €{MontoTotal}",
                request.DeudorId, request.AcreedorId, request.MontoTotal);

            // Validaciones básicas
            if (request.DeudorId == request.AcreedorId)
            {
                throw new ArgumentException("El deudor y acreedor no pueden ser la misma persona");
            }

            // Verificar que ambos usuarios pertenecen al grupo
            var usuarios = await _context.Usuarios
                .Where(u => (u.Id == request.DeudorId || u.Id == request.AcreedorId) &&
                           u.GrupoId == request.GrupoId && u.Activo)
                .ToListAsync();

            if (usuarios.Count != 2)
            {
                throw new ArgumentException("Los usuarios no pertenecen al grupo especificado");
            }

            // Calcular cuota
            var montoCuota = Math.Round(request.MontoTotal / request.NumeroCuotas, 2);
            var montoRestante = request.MontoTotal;

            // Crear el plan
            var plan = new PlanPago
            {
                GrupoId = request.GrupoId,
                DeudorId = request.DeudorId,
                AcreedorId = request.AcreedorId,
                MontoTotal = request.MontoTotal,
                NumeroCuotas = request.NumeroCuotas,
                DiasFrecuencia = request.DiasFrecuencia,
                MontoCuota = montoCuota,
                MontoRestante = request.MontoTotal,
                Descripcion = request.Descripcion,
                Motivo = request.Motivo,
                FechaPrimeraCuota = request.FechaPrimeraCuota,
                EstadoMensualId = request.EstadoMensualId,
                FechaCreacion = DateTime.UtcNow
            };

            _context.PlanesPago.Add(plan);
            await _context.SaveChangesAsync();

            // Crear las cuotas
            await CrearCuotasAsync(plan.Id, request.NumeroCuotas, montoCuota,
                request.FechaPrimeraCuota, request.DiasFrecuencia, request.MontoTotal);

            _logger.LogInformation("Plan de pago {PlanId} creado con {NumeroCuotas} cuotas de €{MontoCuota}",
                plan.Id, request.NumeroCuotas, montoCuota);

            return plan.Id;
        }

        public async Task<List<PlanPagoResponse>> ObtenerPlanesPorGrupoAsync(int grupoId)
        {
            return await _context.PlanesPago
                .Where(p => p.GrupoId == grupoId && p.Activo)
                .Select(p => new PlanPagoResponse
                {
                    Id = p.Id,
                    DeudorNombre = _context.Usuarios.Where(u => u.Id == p.DeudorId).Select(u => u.Alias ?? u.Nombre).FirstOrDefault() ?? "Usuario",
                    AcreedorNombre = _context.Usuarios.Where(u => u.Id == p.AcreedorId).Select(u => u.Alias ?? u.Nombre).FirstOrDefault() ?? "Usuario",
                    MontoTotal = p.MontoTotal,
                    MontoCuota = p.MontoCuota,
                    NumeroCuotas = p.NumeroCuotas,
                    CuotasPagadas = p.CuotasPagadas,
                    MontoPagado = p.MontoPagado,
                    MontoRestante = p.MontoRestante,
                    Descripcion = p.Descripcion,
                    FechaPrimeraCuota = p.FechaPrimeraCuota,
                    FechaCompletado = p.FechaCompletado,
                    Completado = p.Completado,
                    EsSoyDeudor = false, // Se calculará en el controlador
                    PorcentajeCompletado = p.MontoTotal > 0 ? Math.Round((p.MontoPagado / p.MontoTotal) * 100, 1) : 0
                })
                .OrderByDescending(p => p.FechaPrimeraCuota)
                .ToListAsync();
        }

        public async Task<List<PlanPagoResponse>> ObtenerPlanesPorUsuarioAsync(int usuarioId)
        {
            return await _context.PlanesPago
                .Where(p => (p.DeudorId == usuarioId || p.AcreedorId == usuarioId) && p.Activo)
                .Select(p => new PlanPagoResponse
                {
                    Id = p.Id,
                    DeudorNombre = _context.Usuarios.Where(u => u.Id == p.DeudorId).Select(u => u.Alias ?? u.Nombre).FirstOrDefault() ?? "Usuario",
                    AcreedorNombre = _context.Usuarios.Where(u => u.Id == p.AcreedorId).Select(u => u.Alias ?? u.Nombre).FirstOrDefault() ?? "Usuario",
                    MontoTotal = p.MontoTotal,
                    MontoCuota = p.MontoCuota,
                    NumeroCuotas = p.NumeroCuotas,
                    CuotasPagadas = p.CuotasPagadas,
                    MontoPagado = p.MontoPagado,
                    MontoRestante = p.MontoRestante,
                    Descripcion = p.Descripcion,
                    FechaPrimeraCuota = p.FechaPrimeraCuota,
                    FechaCompletado = p.FechaCompletado,
                    Completado = p.Completado,
                    EsSoyDeudor = p.DeudorId == usuarioId,
                    ProximaCuotaFecha = _context.CuotasPago
                        .Where(c => c.PlanPagoId == p.Id && c.Estado == EstadoCuota.Pendiente)
                        .OrderBy(c => c.FechaVencimiento)
                        .Select(c => (DateTime?)c.FechaVencimiento)
                        .FirstOrDefault(),
                    PorcentajeCompletado = p.MontoTotal > 0 ? Math.Round((p.MontoPagado / p.MontoTotal) * 100, 1) : 0
                })
                .OrderBy(p => p.Completado)
                .ThenBy(p => p.ProximaCuotaFecha)
                .ToListAsync();
        }

        public async Task<PlanPagoDetalleResponse?> ObtenerDetallePlanAsync(int planId)
        {
            var plan = await _context.PlanesPago
                .Include(p => p.Cuotas)
                .FirstOrDefaultAsync(p => p.Id == planId);

            if (plan == null) return null;

            var deudor = await _context.Usuarios.FindAsync(plan.DeudorId);
            var acreedor = await _context.Usuarios.FindAsync(plan.AcreedorId);
            var grupo = await _context.Grupos.FindAsync(plan.GrupoId);

            return new PlanPagoDetalleResponse
            {
                Id = plan.Id,
                GrupoNombre = grupo?.Nombre ?? "Grupo",
                DeudorId = plan.DeudorId,
                DeudorNombre = deudor?.Alias ?? deudor?.Nombre ?? "Usuario",
                DeudorColor = deudor?.ColorPersonalizado ?? "#757575",
                AcreedorId = plan.AcreedorId,
                AcreedorNombre = acreedor?.Alias ?? acreedor?.Nombre ?? "Usuario",
                AcreedorColor = acreedor?.ColorPersonalizado ?? "#757575",
                MontoTotal = plan.MontoTotal,
                MontoCuota = plan.MontoCuota,
                NumeroCuotas = plan.NumeroCuotas,
                DiasFrecuencia = plan.DiasFrecuencia,
                CuotasPagadas = plan.CuotasPagadas,
                MontoPagado = plan.MontoPagado,
                MontoRestante = plan.MontoRestante,
                Descripcion = plan.Descripcion,
                Motivo = plan.Motivo,
                FechaCreacion = plan.FechaCreacion,
                FechaPrimeraCuota = plan.FechaPrimeraCuota,
                FechaCompletado = plan.FechaCompletado,
                Activo = plan.Activo,
                Completado = plan.Completado,
                EstadoMensualId = plan.EstadoMensualId,
                PorcentajeCompletado = plan.MontoTotal > 0 ? Math.Round((plan.MontoPagado / plan.MontoTotal) * 100, 1) : 0,
                Cuotas = plan.Cuotas.Select(c => new CuotaResponse
                {
                    Id = c.Id,
                    NumeroCuota = c.NumeroCuota,
                    Monto = c.Monto,
                    FechaVencimiento = c.FechaVencimiento,
                    FechaPago = c.FechaPago,
                    Estado = c.Estado.ToString(),
                    MetodoPago = c.MetodoPago,
                    NotasPago = c.NotasPago,
                    Confirmado = c.Confirmado,
                    DiasRestantes = (int)(c.FechaVencimiento - DateTime.Now).TotalDays,
                    EstaVencida = c.FechaVencimiento < DateTime.Now && c.Estado == EstadoCuota.Pendiente
                }).OrderBy(c => c.NumeroCuota).ToList()
            };
        }

        public async Task<bool> PagarCuotaAsync(PagarCuotaRequest request)
        {
            var cuota = await _context.CuotasPago
                .Include(c => c.PlanPago)
                .FirstOrDefaultAsync(c => c.Id == request.CuotaId);

            if (cuota == null || cuota.Estado != EstadoCuota.Pendiente)
            {
                _logger.LogWarning("Cuota {CuotaId} no encontrada o no está pendiente", request.CuotaId);
                return false;
            }

            // Marcar cuota como pagada
            cuota.Estado = EstadoCuota.Pagada;
            cuota.FechaPago = DateTime.UtcNow;
            cuota.MetodoPago = request.MetodoPago;
            cuota.NotasPago = request.Notas;
            cuota.ComprobantePago = request.ComprobantePago;

            // Actualizar estadísticas del plan
            if (cuota.PlanPago != null)
            {
                cuota.PlanPago.CuotasPagadas++;
                cuota.PlanPago.MontoPagado += cuota.Monto;
                cuota.PlanPago.MontoRestante = cuota.PlanPago.MontoTotal - cuota.PlanPago.MontoPagado;

                // Verificar si el plan está completado
                if (cuota.PlanPago.MontoPagado >= cuota.PlanPago.MontoTotal)
                {
                    cuota.PlanPago.Completado = true;
                    cuota.PlanPago.FechaCompletado = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cuota {CuotaId} pagada exitosamente via {MetodoPago}",
                request.CuotaId, request.MetodoPago);

            return true;
        }

        public async Task<bool> ConfirmarCuotaAsync(int cuotaId, int usuarioId)
        {
            var cuota = await _context.CuotasPago
                .Include(c => c.PlanPago)
                .FirstOrDefaultAsync(c => c.Id == cuotaId);

            if (cuota == null || cuota.Estado != EstadoCuota.Pagada)
            {
                _logger.LogWarning("Cuota {CuotaId} no encontrada o no está pagada", cuotaId);
                return false;
            }

            // Verificar que el usuario es el acreedor
            if (cuota.PlanPago?.AcreedorId != usuarioId)
            {
                _logger.LogWarning("Usuario {UsuarioId} no es el acreedor de la cuota {CuotaId}",
                    usuarioId, cuotaId);
                return false;
            }

            // Confirmar cuota
            cuota.Estado = EstadoCuota.Confirmada;
            cuota.Confirmado = true;
            cuota.FechaConfirmacion = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cuota {CuotaId} confirmada por usuario {UsuarioId}", cuotaId, usuarioId);

            return true;
        }

        public async Task<List<CuotaPendienteResponse>> ObtenerCuotasPendientesAsync(int usuarioId)
        {
            var fechaActual = DateTime.Now;

            return await _context.CuotasPago
                .Include(c => c.PlanPago)
                .Where(c => (c.PlanPago!.DeudorId == usuarioId || c.PlanPago.AcreedorId == usuarioId) &&
                           c.Estado != EstadoCuota.Confirmada &&
                           c.Estado != EstadoCuota.Cancelada &&
                           c.PlanPago.Activo)
                .Select(c => new CuotaPendienteResponse
                {
                    CuotaId = c.Id,
                    PlanId = c.PlanPagoId,
                    NumeroCuota = c.NumeroCuota,
                    Monto = c.Monto,
                    FechaVencimiento = c.FechaVencimiento,
                    Estado = c.Estado.ToString(),
                    DeudorNombre = _context.Usuarios.Where(u => u.Id == c.PlanPago!.DeudorId).Select(u => u.Alias ?? u.Nombre).FirstOrDefault() ?? "Usuario",
                    AcreedorNombre = _context.Usuarios.Where(u => u.Id == c.PlanPago!.AcreedorId).Select(u => u.Alias ?? u.Nombre).FirstOrDefault() ?? "Usuario",
                    Descripcion = c.PlanPago!.Descripcion,
                    EsSoyDeudor = c.PlanPago!.DeudorId == usuarioId,
                    RequiereAccion = (c.PlanPago!.DeudorId == usuarioId && c.Estado == EstadoCuota.Pendiente) ||
                                   (c.PlanPago.AcreedorId == usuarioId && c.Estado == EstadoCuota.Pagada),
                    DiasRestantes = (int)(c.FechaVencimiento - fechaActual).TotalDays,
                    EstaVencida = c.FechaVencimiento < fechaActual && c.Estado == EstadoCuota.Pendiente
                })
                .OrderBy(c => c.FechaVencimiento)
                .ToListAsync();
        }

        public async Task<bool> CompletarPlanAsync(int planId)
        {
            var plan = await _context.PlanesPago.FindAsync(planId);
            if (plan == null || plan.Completado)
            {
                return false;
            }

            plan.Completado = true;
            plan.FechaCompletado = DateTime.UtcNow;

            // Marcar todas las cuotas pendientes como completadas
            var cuotasPendientes = await _context.CuotasPago
                .Where(c => c.PlanPagoId == planId && c.Estado == EstadoCuota.Pendiente)
                .ToListAsync();

            foreach (var cuota in cuotasPendientes)
            {
                cuota.Estado = EstadoCuota.Confirmada;
                cuota.FechaPago = DateTime.UtcNow;
                cuota.Confirmado = true;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Plan {PlanId} completado manualmente", planId);

            return true;
        }

        public async Task<bool> CancelarPlanAsync(int planId)
        {
            var plan = await _context.PlanesPago.FindAsync(planId);
            if (plan == null || plan.Completado)
            {
                return false;
            }

            plan.Activo = false;

            // Cancelar todas las cuotas pendientes
            var cuotasPendientes = await _context.CuotasPago
                .Where(c => c.PlanPagoId == planId &&
                           (c.Estado == EstadoCuota.Pendiente || c.Estado == EstadoCuota.Vencida))
                .ToListAsync();

            foreach (var cuota in cuotasPendientes)
            {
                cuota.Estado = EstadoCuota.Cancelada;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Plan {PlanId} cancelado", planId);

            return true;
        }

        // ===== MÉTODOS PRIVADOS AUXILIARES =====

        private async Task CrearCuotasAsync(int planId, int numeroCuotas, decimal montoCuota,
            DateTime fechaPrimera, int diasFrecuencia, decimal montoTotal)
        {
            var cuotas = new List<CuotaPago>();
            var fechaVencimiento = fechaPrimera;
            var montoAcumulado = 0m;

            for (int i = 1; i <= numeroCuotas; i++)
            {
                // Para la última cuota, ajustar el monto para cubrir cualquier diferencia por redondeo
                var montoCuotaActual = i == numeroCuotas ?
                    montoTotal - montoAcumulado : montoCuota;

                var cuota = new CuotaPago
                {
                    PlanPagoId = planId,
                    NumeroCuota = i,
                    Monto = montoCuotaActual,
                    FechaVencimiento = fechaVencimiento,
                    Estado = EstadoCuota.Pendiente,
                    FechaCreacion = DateTime.UtcNow
                };

                cuotas.Add(cuota);
                montoAcumulado += montoCuotaActual;

                // Calcular siguiente fecha de vencimiento
                fechaVencimiento = fechaVencimiento.AddDays(diasFrecuencia);
            }

            _context.CuotasPago.AddRange(cuotas);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Creadas {NumeroCuotas} cuotas para plan {PlanId}",
                numeroCuotas, planId);
        }
    }
}