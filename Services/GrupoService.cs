using GastosHogarAPI.Data;
using GastosHogarAPI.Models;
using GastosHogarAPI.Models.DTOs;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace GastosHogarAPI.Services
{
    public class GrupoService : IGrupoService
    {
        private readonly AppDbContext _context;

        public GrupoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<GrupoResponse> CrearGrupoAsync(CrearGrupoRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(request.UsuarioId);
            if (usuario == null)
            {
                return new GrupoResponse { Exito = false, Mensaje = "Usuario no encontrado" };
            }

            if (usuario.GrupoId != null)
            {
                return new GrupoResponse { Exito = false, Mensaje = "Ya perteneces a un grupo" };
            }

            var codigo = await GenerarCodigoUnicoAsync();

            var grupo = new Grupo
            {
                Nombre = request.Nombre,
                Descripcion = request.Descripcion,
                CreadorId = request.UsuarioId,
                CodigoInvitacion = codigo,
                FechaCreacion = DateTime.Now,
                UltimaActividad = DateTime.Now
            };

            _context.Grupos.Add(grupo);
            await _context.SaveChangesAsync();

            // Agregar usuario al grupo
            usuario.GrupoId = grupo.Id;
            await _context.SaveChangesAsync();

            return new GrupoResponse
            {
                Exito = true,
                GrupoId = grupo.Id,
                NombreGrupo = grupo.Nombre,
                CodigoInvitacion = grupo.CodigoInvitacion,
                Mensaje = $"Grupo '{grupo.Nombre}' creado exitosamente",
                Miembros = new List<MiembroGrupo>
                {
                    new MiembroGrupo
                    {
                        Id = usuario.Id,
                        Nombre = usuario.Nombre,
                        Alias = usuario.Alias,
                        Email = usuario.Email,
                        FotoUrl = usuario.FotoUrl,
                        ColorPersonalizado = usuario.ColorPersonalizado,
                        EsCreador = true,
                        FechaIngreso = DateTime.Now,
                        UltimoAcceso = usuario.UltimoAcceso,
                        TotalGastado = usuario.TotalGastado,
                        GastosCreados = usuario.GastosCreados
                    }
                }
            };
        }

        public async Task<GrupoResponse> UnirseGrupoAsync(UnirseGrupoRequest request)
        {
            var usuario = await _context.Usuarios.FindAsync(request.UsuarioId);
            if (usuario == null)
            {
                return new GrupoResponse { Exito = false, Mensaje = "Usuario no encontrado" };
            }

            if (usuario.GrupoId != null)
            {
                return new GrupoResponse { Exito = false, Mensaje = "Ya perteneces a un grupo" };
            }

            var grupo = await _context.Grupos
                .Include(g => g.Miembros)
                .FirstOrDefaultAsync(g => g.CodigoInvitacion == request.CodigoInvitacion && g.Activo);

            if (grupo == null)
            {
                return new GrupoResponse { Exito = false, Mensaje = "Código de invitación inválido" };
            }

            usuario.GrupoId = grupo.Id;
            grupo.UltimaActividad = DateTime.Now;
            await _context.SaveChangesAsync();

            var miembros = await ObtenerMiembrosGrupoAsync(grupo.Id);

            return new GrupoResponse
            {
                Exito = true,
                GrupoId = grupo.Id,
                NombreGrupo = grupo.Nombre,
                Mensaje = $"Te has unido al grupo '{grupo.Nombre}'",
                Miembros = miembros
            };
        }

        public async Task<GrupoResponse> UnirseGrupoUsuarioAsync(UnirseGrupoUsuarioRequest request)
        {
            var usuarioActual = await _context.Usuarios.FindAsync(request.UsuarioId);
            var usuarioDestino = await _context.Usuarios
                .Include(u => u.Grupo)
                .FirstOrDefaultAsync(u => u.Id == request.UsuarioDestinoId);

            if (usuarioActual == null || usuarioDestino == null)
            {
                return new GrupoResponse { Exito = false, Mensaje = "Usuario no encontrado" };
            }

            if (usuarioActual.GrupoId != null)
            {
                return new GrupoResponse { Exito = false, Mensaje = "Ya perteneces a un grupo" };
            }

            if (usuarioDestino.GrupoId == null)
            {
                return new GrupoResponse { Exito = false, Mensaje = "El usuario destino no pertenece a ningún grupo" };
            }

            usuarioActual.GrupoId = usuarioDestino.GrupoId;
            usuarioDestino.Grupo!.UltimaActividad = DateTime.Now;
            await _context.SaveChangesAsync();

            var miembros = await ObtenerMiembrosGrupoAsync(usuarioDestino.GrupoId.Value);

            return new GrupoResponse
            {
                Exito = true,
                GrupoId = usuarioDestino.GrupoId.Value,
                NombreGrupo = usuarioDestino.Grupo.Nombre,
                Mensaje = $"Te has unido al grupo '{usuarioDestino.Grupo.Nombre}' junto con {usuarioDestino.Nombre}",
                Miembros = miembros
            };
        }

        public async Task<bool> SalirGrupoAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null || usuario.GrupoId == null) return false;

            usuario.GrupoId = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<MiembroGrupo>> ObtenerMiembrosGrupoAsync(int grupoId)
        {
            return await _context.Usuarios
                .Where(u => u.GrupoId == grupoId && u.Activo)
                .Select(u => new MiembroGrupo
                {
                    Id = u.Id,
                    Nombre = u.Nombre,
                    Alias = u.Alias,
                    Email = u.Email,
                    FotoUrl = u.FotoUrl,
                    ColorPersonalizado = u.ColorPersonalizado,
                    EsCreador = u.Grupo!.CreadorId == u.Id,
                    FechaIngreso = u.FechaCreacion,
                    UltimoAcceso = u.UltimoAcceso,
                    TotalGastado = u.TotalGastado,
                    GastosCreados = u.GastosCreados
                })
                .OrderBy(m => m.EsCreador ? 0 : 1)
                .ThenBy(m => m.FechaIngreso)
                .ToListAsync();
        }

        private async Task<string> GenerarCodigoUnicoAsync()
        {
            string codigo;
            bool existe;

            do
            {
                codigo = GenerarCodigoAleatorio();
                existe = await _context.Grupos.AnyAsync(g => g.CodigoInvitacion == codigo);
            } while (existe);

            return codigo;
        }

        private static string GenerarCodigoAleatorio()
        {
            const string caracteres = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(caracteres, 6)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}