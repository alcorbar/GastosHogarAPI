using GastosHogarAPI.Data;
using GastosHogarAPI.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GastosHogarAPI.Services
{
    public class BusinessValidationService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BusinessValidationService> _logger;

        public BusinessValidationService(AppDbContext context, ILogger<BusinessValidationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Valida que un usuario existe y está activo
        /// </summary>
        public async Task<Models.Usuario> ValidarUsuarioExisteAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);

            if (usuario == null)
            {
                throw new NotFoundException("Usuario", usuarioId);
            }

            if (!usuario.Activo)
            {
                throw new ForbiddenException("El usuario está desactivado");
            }

            return usuario;
        }

        /// <summary>
        /// Valida que un usuario pertenece a un grupo específico
        /// </summary>
        public async Task ValidarUsuarioPerteneceGrupoAsync(int usuarioId, int grupoId)
        {
            var usuario = await ValidarUsuarioExisteAsync(usuarioId);

            if (usuario.GrupoId != grupoId)
            {
                throw new ForbiddenException("El usuario no pertenece al grupo especificado");
            }
        }

        /// <summary>
        /// Valida que un grupo existe y está activo
        /// </summary>
        public async Task<Models.Grupo> ValidarGrupoExisteAsync(int grupoId)
        {
            var grupo = await _context.Grupos.FindAsync(grupoId);

            if (grupo == null)
            {
                throw new NotFoundException("Grupo", grupoId);
            }

            if (!grupo.Activo)
            {
                throw new ForbiddenException("El grupo está desactivado");
            }

            return grupo;
        }

        /// <summary>
        /// Valida que una categoría existe y está disponible para el grupo
        /// </summary>
        public async Task<Models.Categoria> ValidarCategoriaDisponibleAsync(int categoriaId, int grupoId)
        {
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == categoriaId && c.Activa);

            if (categoria == null)
            {
                throw new NotFoundException("Categoría", categoriaId);
            }

            // La categoría debe ser predeterminada O pertenecer al grupo
            if (!categoria.EsPredeterminada && categoria.GrupoId != grupoId)
            {
                throw new ForbiddenException("La categoría no está disponible para este grupo");
            }

            return categoria;
        }

        /// <summary>
        /// Valida que un gasto existe y pertenece al grupo del usuario
        /// </summary>
        public async Task<Models.Gasto> ValidarGastoAccesoAsync(int gastoId, int usuarioId)
        {
            var usuario = await ValidarUsuarioExisteAsync(usuarioId);

            var gasto = await _context.Gastos
                .Include(g => g.Usuario)
                .Include(g => g.Grupo)
                .FirstOrDefaultAsync(g => g.Id == gastoId);

            if (gasto == null)
            {
                throw new NotFoundException("Gasto", gastoId);
            }

            // El usuario debe pertenecer al mismo grupo que el gasto
            if (gasto.GrupoId != usuario.GrupoId)
            {
                throw new ForbiddenException("No tienes acceso a este gasto");
            }

            return gasto;
        }

        /// <summary>
        /// Valida que un usuario puede modificar un gasto (es el creador o admin)
        /// </summary>
        public async Task ValidarPermisoModificarGastoAsync(int gastoId, int usuarioId)
        {
            var gasto = await ValidarGastoAccesoAsync(gastoId, usuarioId);

            // Solo el creador del gasto puede modificarlo (o podrías agregar lógica de admin)
            if (gasto.UsuarioId != usuarioId)
            {
                throw new ForbiddenException("Solo el creador del gasto puede modificarlo");
            }
        }

        /// <summary>
        /// Valida que un plan de pago existe y el usuario tiene acceso
        /// </summary>
        public async Task<Models.PlanPago> ValidarPlanPagoAccesoAsync(int planId, int usuarioId)
        {
            var usuario = await ValidarUsuarioExisteAsync(usuarioId);

            var plan = await _context.PlanesPago
                .FirstOrDefaultAsync(p => p.Id == planId && p.Activo);

            if (plan == null)
            {
                throw new NotFoundException("Plan de pago", planId);
            }

            // El usuario debe ser deudor, acreedor, o pertenecer al grupo
            var tieneAcceso = plan.DeudorId == usuarioId ||
                             plan.AcreedorId == usuarioId ||
                             plan.GrupoId == usuario.GrupoId;

            if (!tieneAcceso)
            {
                throw new ForbiddenException("No tienes acceso a este plan de pago");
            }

            return plan;
        }

        /// <summary>
        /// Valida que una cuota existe y el usuario puede interactuar con ella
        /// </summary>
        public async Task<Models.CuotaPago> ValidarCuotaAccesoAsync(int cuotaId, int usuarioId)
        {
            var cuota = await _context.CuotasPago
                .Include(c => c.PlanPago)
                .FirstOrDefaultAsync(c => c.Id == cuotaId);

            if (cuota == null)
            {
                throw new NotFoundException("Cuota", cuotaId);
            }

            if (cuota.PlanPago == null)
            {
                throw new BadRequestException("La cuota no tiene un plan de pago asociado");
            }

            // Validar acceso al plan de pago
            await ValidarPlanPagoAccesoAsync(cuota.PlanPagoId, usuarioId);

            return cuota;
        }

        /// <summary>
        /// Valida que un usuario puede pagar una cuota (es el deudor)
        /// </summary>
        public async Task ValidarPermisoPagarCuotaAsync(int cuotaId, int usuarioId)
        {
            var cuota = await ValidarCuotaAccesoAsync(cuotaId, usuarioId);

            if (cuota.PlanPago!.DeudorId != usuarioId)
            {
                throw new ForbiddenException("Solo el deudor puede pagar esta cuota");
            }

            if (cuota.Estado != Models.EstadoCuota.Pendiente && cuota.Estado != Models.EstadoCuota.Vencida)
            {
                throw new BadRequestException("La cuota no está en estado para ser pagada");
            }
        }

        /// <summary>
        /// Valida que un usuario puede confirmar una cuota (es el acreedor)
        /// </summary>
        public async Task ValidarPermisoConfirmarCuotaAsync(int cuotaId, int usuarioId)
        {
            var cuota = await ValidarCuotaAccesoAsync(cuotaId, usuarioId);

            if (cuota.PlanPago!.AcreedorId != usuarioId)
            {
                throw new ForbiddenException("Solo el acreedor puede confirmar esta cuota");
            }

            if (cuota.Estado != Models.EstadoCuota.Pagada)
            {
                throw new BadRequestException("La cuota debe estar pagada para poder confirmarla");
            }
        }

        /// <summary>
        /// Valida que un código de invitación es válido
        /// </summary>
        public async Task<Models.Grupo> ValidarCodigoInvitacionAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo) || codigo.Length != 6)
            {
                throw new BadRequestException("El código de invitación debe tener 6 caracteres");
            }

            var grupo = await _context.Grupos
                .FirstOrDefaultAsync(g => g.CodigoInvitacion == codigo.ToUpper() && g.Activo);

            if (grupo == null)
            {
                throw new NotFoundException("Código de invitación inválido o expirado");
            }

            return grupo;
        }

        /// <summary>
        /// Valida que un usuario puede unirse a un grupo (no pertenece a otro)
        /// </summary>
        public async Task ValidarPuedeUnirseGrupoAsync(int usuarioId)
        {
            var usuario = await ValidarUsuarioExisteAsync(usuarioId);

            if (usuario.GrupoId != null)
            {
                throw new ConflictException("Ya perteneces a un grupo. Debes salir del grupo actual primero");
            }
        }

        /// <summary>
        /// Valida que un usuario puede crear un grupo (no pertenece a otro)
        /// </summary>
        public async Task ValidarPuedeCrearGrupoAsync(int usuarioId)
        {
            await ValidarPuedeUnirseGrupoAsync(usuarioId); // Misma validación
        }

        /// <summary>
        /// Valida límites de negocio para gastos
        /// </summary>
        public void ValidarLimitesGasto(decimal importe, string descripcion)
        {
            if (importe <= 0)
            {
                throw new BadRequestException("El importe debe ser mayor que cero");
            }

            if (importe > 999999.99m)
            {
                throw new BadRequestException("El importe excede el límite máximo permitido");
            }

            if (string.IsNullOrWhiteSpace(descripcion))
            {
                throw new BadRequestException("La descripción es obligatoria");
            }

            if (descripcion.Length > 500)
            {
                throw new BadRequestException("La descripción es demasiado larga");
            }
        }

        /// <summary>
        /// Valida límites de negocio para planes de pago
        /// </summary>
        public void ValidarLimitesPlanPago(decimal montoTotal, int numeroCuotas, int diasFrecuencia)
        {
            if (montoTotal <= 0)
            {
                throw new BadRequestException("El monto total debe ser mayor que cero");
            }

            if (montoTotal > 999999.99m)
            {
                throw new BadRequestException("El monto total excede el límite máximo permitido");
            }

            if (numeroCuotas < 2 || numeroCuotas > 12)
            {
                throw new BadRequestException("El número de cuotas debe estar entre 2 y 12");
            }

            if (diasFrecuencia < 1 || diasFrecuencia > 365)
            {
                throw new BadRequestException("La frecuencia debe estar entre 1 y 365 días");
            }

            var montoCuota = montoTotal / numeroCuotas;
            if (montoCuota < 0.01m)
            {
                throw new BadRequestException("El monto de cada cuota es demasiado pequeño");
            }
        }

        /// <summary>
        /// Valida que los participantes de un plan de pago son válidos
        /// </summary>
        public async Task ValidarParticipantesPlanPagoAsync(int deudorId, int acreedorId, int grupoId)
        {
            if (deudorId == acreedorId)
            {
                throw new BadRequestException("El deudor y acreedor no pueden ser la misma persona");
            }

            // Validar que ambos pertenecen al grupo
            await ValidarUsuarioPerteneceGrupoAsync(deudorId, grupoId);
            await ValidarUsuarioPerteneceGrupoAsync(acreedorId, grupoId);
        }

        /// <summary>
        /// Valida que un período (mes/año) es válido
        /// </summary>
        public void ValidarPeriodo(int mes, int año)
        {
            if (mes < 1 || mes > 12)
            {
                throw new BadRequestException("El mes debe estar entre 1 y 12");
            }

            if (año < 2020 || año > DateTime.Now.Year + 1)
            {
                throw new BadRequestException($"El año debe estar entre 2020 y {DateTime.Now.Year + 1}");
            }
        }

        /// <summary>
        /// Valida que una liquidación puede ser modificada
        /// </summary>
        public async Task ValidarLiquidacionModificableAsync(int grupoId, int mes, int año)
        {
            ValidarPeriodo(mes, año);

            var estado = await _context.EstadoMensual
                .FirstOrDefaultAsync(e => e.GrupoId == grupoId && e.Mes == mes && e.Año == año);

            if (estado?.EstadoPago == "confirmado")
            {
                throw new ConflictException("La liquidación ya ha sido confirmada y no puede modificarse");
            }
        }

        /// <summary>
        /// Valida que un email es único en el sistema
        /// </summary>
        public async Task ValidarEmailUnicoAsync(string email, int? excluirUsuarioId = null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new BadRequestException("El email es obligatorio");
            }

            var query = _context.Usuarios.Where(u => u.Email.ToLower() == email.ToLower() && u.Activo);

            if (excluirUsuarioId.HasValue)
            {
                query = query.Where(u => u.Id != excluirUsuarioId.Value);
            }

            var existe = await query.AnyAsync();

            if (existe)
            {
                throw new ConflictException("Ya existe un usuario con este email");
            }
        }

        /// <summary>
        /// Valida que un nombre de categoría es único en el contexto (grupo o predeterminadas)
        /// </summary>
        public async Task ValidarNombreCategoriaUnicoAsync(string nombre, int grupoId, int? excluirCategoriaId = null)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                throw new BadRequestException("El nombre de la categoría es obligatorio");
            }

            var query = _context.Categorias.Where(c =>
                c.Nombre.ToLower() == nombre.ToLower() &&
                c.Activa &&
                (c.EsPredeterminada || c.GrupoId == grupoId));

            if (excluirCategoriaId.HasValue)
            {
                query = query.Where(c => c.Id != excluirCategoriaId.Value);
            }

            var existe = await query.AnyAsync();

            if (existe)
            {
                throw new ConflictException("Ya existe una categoría con este nombre");
            }
        }
    }
}