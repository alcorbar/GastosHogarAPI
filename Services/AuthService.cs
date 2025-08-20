using GastosHogarAPI.Data;
using GastosHogarAPI.Models;
using GastosHogarAPI.Models.DTOs;
using GastosHogarAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using LoginRequest = GastosHogarAPI.Models.DTOs.LoginRequest;

namespace GastosHogarAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthService(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        // ACTUALIZADO: Login tradicional con PIN + JWT
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Grupo)
                .FirstOrDefaultAsync(u => u.Nombre == request.Usuario && u.Activo);

            if (usuario == null)
            {
                return new LoginResponse { Exito = false, Mensaje = "Usuario no encontrado" };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Pin, usuario.Pin))
            {
                return new LoginResponse { Exito = false, Mensaje = "PIN incorrecto" };
            }

            // Actualizar último acceso
            usuario.UltimoAcceso = DateTime.Now;
            await _context.SaveChangesAsync();

            // Generar token JWT
            var token = _jwtService.GenerateToken(usuario.Id, usuario.Nombre, usuario.Email, usuario.GrupoId);
            var tokenExpiration = DateTime.UtcNow.AddHours(24);

            return new LoginResponse
            {
                Exito = true,
                Mensaje = "Login exitoso",
                UsuarioId = usuario.Id,
                NombreUsuario = usuario.Nombre,
                Email = usuario.Email,
                Alias = usuario.Alias,
                FotoUrl = usuario.FotoUrl,
                ColorPersonalizado = usuario.ColorPersonalizado,
                Token = token,
                TokenExpiration = tokenExpiration,
                Grupo = usuario.Grupo != null ? new GrupoInfo
                {
                    Id = usuario.Grupo.Id,
                    Nombre = usuario.Grupo.Nombre,
                    Descripcion = usuario.Grupo.Descripcion,
                    EsCreador = usuario.Grupo.CreadorId == usuario.Id
                } : null,
                RequiereGrupo = usuario.GrupoId == null
            };
        }

        // ACTUALIZADO: Login con Google + JWT
        public async Task<LoginResponse> GoogleLoginAsync(GoogleLoginRequest request)
        {
            var googleUser = await VerifyGoogleTokenAsync(request.IdToken);

            if (googleUser == null)
            {
                return new LoginResponse { Exito = false, Mensaje = "Token de Google inválido" };
            }

            var usuario = await _context.Usuarios
                .Include(u => u.Grupo)
                .FirstOrDefaultAsync(u => u.Email == googleUser.Email && u.Activo);

            if (usuario != null)
            {
                await VincularDispositivoSiNoExisteAsync(usuario.Id, request);
                usuario.UltimoAcceso = DateTime.Now;
                await _context.SaveChangesAsync();

                return CrearLoginResponse(usuario, false);
            }
            else
            {
                var nuevoUsuario = new Usuario
                {
                    Nombre = googleUser.Name,
                    Email = googleUser.Email,
                    GoogleId = googleUser.Id,
                    FotoUrl = googleUser.Picture,
                    Pin = "",
                    ColorPersonalizado = GenerarColorAleatorio()
                };

                _context.Usuarios.Add(nuevoUsuario);
                await _context.SaveChangesAsync();

                await VincularDispositivoAsync(nuevoUsuario.Id, request);

                return CrearLoginResponse(nuevoUsuario, true);
            }
        }

        // ACTUALIZADO: Auto-login + JWT
        public async Task<LoginResponse> AutoLoginAsync(AutoLoginRequest request)
        {
            var dispositivo = await _context.DispositivosUsuario
                .Include(d => d.Usuario)
                .ThenInclude(u => u!.Grupo)
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId && d.Activo);

            if (dispositivo?.Usuario == null)
            {
                return new LoginResponse { Exito = false, Mensaje = "Dispositivo no reconocido" };
            }

            dispositivo.UltimoAcceso = DateTime.Now;
            dispositivo.Usuario.UltimoAcceso = DateTime.Now;
            await _context.SaveChangesAsync();

            return CrearLoginResponse(dispositivo.Usuario, false);
        }

        public async Task<UsuarioBusquedaResponse> BuscarUsuarioPorEmailAsync(string email)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Grupo)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.Activo);

            if (usuario == null)
            {
                return new UsuarioBusquedaResponse
                {
                    Encontrado = false,
                    Mensaje = "No existe ningún usuario con ese email"
                };
            }

            if (usuario.GrupoId == null)
            {
                return new UsuarioBusquedaResponse
                {
                    Encontrado = true,
                    TieneGrupo = false,
                    UsuarioId = usuario.Id,
                    Nombre = usuario.Nombre,
                    Email = usuario.Email,
                    Alias = usuario.Alias,
                    FotoUrl = usuario.FotoUrl,
                    ColorPersonalizado = usuario.ColorPersonalizado,
                    Mensaje = $"{usuario.Nombre} aún no pertenece a ningún grupo"
                };
            }

            return new UsuarioBusquedaResponse
            {
                Encontrado = true,
                TieneGrupo = true,
                UsuarioId = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Alias = usuario.Alias,
                FotoUrl = usuario.FotoUrl,
                ColorPersonalizado = usuario.ColorPersonalizado,
                GrupoId = usuario.GrupoId,
                NombreGrupo = usuario.Grupo?.Nombre,
                CodigoGrupo = usuario.Grupo?.CodigoInvitacion,
                Mensaje = $"{usuario.Nombre} pertenece al grupo '{usuario.Grupo?.Nombre}'"
            };
        }

        public async Task<bool> CambiarPinAsync(int usuarioId, string nuevoPin)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null) return false;

            usuario.Pin = BCrypt.Net.BCrypt.HashPassword(nuevoPin);
            await _context.SaveChangesAsync();
            return true;
        }

        // MÉTODOS PRIVADOS DE APOYO
        private Task<GoogleUser?> VerifyGoogleTokenAsync(string idToken)
        {
            var testTokens = new Dictionary<string, GoogleUser>
            {
                ["test_token_user1"] = new GoogleUser
                {
                    Id = "google_user1",
                    Name = "Usuario Prueba 1",
                    Email = "usuario1@example.com",
                    Picture = "https://example.com/photo1.jpg"
                },
                ["test_token_user2"] = new GoogleUser
                {
                    Id = "google_user2",
                    Name = "Usuario Prueba 2",
                    Email = "usuario2@example.com",
                    Picture = "https://example.com/photo2.jpg"
                },
                ["test_token_user3"] = new GoogleUser
                {
                    Id = "google_user3",
                    Name = "Usuario Prueba 3",
                    Email = "usuario3@example.com",
                    Picture = "https://example.com/photo3.jpg"
                },
                ["test_token_user4"] = new GoogleUser
                {
                    Id = "google_user4",
                    Name = "Usuario Prueba 4",
                    Email = "usuario4@example.com",
                    Picture = "https://example.com/photo4.jpg"
                },
                ["test_token_user5"] = new GoogleUser
                {
                    Id = "google_user5",
                    Name = "Usuario Prueba 5",
                    Email = "usuario5@example.com",
                    Picture = "https://example.com/photo5.jpg"
                }
            };

            testTokens.TryGetValue(idToken, out var testUser);
            return Task.FromResult(testUser);
        }

        private async Task VincularDispositivoAsync(int usuarioId, GoogleLoginRequest request)
        {
            var dispositivo = new DispositivoUsuario
            {
                UsuarioId = usuarioId,
                DeviceId = request.DeviceId,
                NombreDispositivo = request.DeviceName,
                TipoDispositivo = request.TipoDispositivo,
                VersionSO = request.VersionSO
            };

            _context.DispositivosUsuario.Add(dispositivo);
            await _context.SaveChangesAsync();
        }

        private async Task VincularDispositivoSiNoExisteAsync(int usuarioId, GoogleLoginRequest request)
        {
            var existe = await _context.DispositivosUsuario
                .AnyAsync(d => d.UsuarioId == usuarioId && d.DeviceId == request.DeviceId);

            if (!existe)
            {
                await VincularDispositivoAsync(usuarioId, request);
            }
        }

        private LoginResponse CrearLoginResponse(Usuario usuario, bool esNuevo)
        {
            var token = _jwtService.GenerateToken(usuario.Id, usuario.Nombre, usuario.Email, usuario.GrupoId);
            var tokenExpiration = DateTime.UtcNow.AddHours(24);

            return new LoginResponse
            {
                Exito = true,
                Mensaje = esNuevo ? "Registro exitoso" : "Login exitoso",
                UsuarioId = usuario.Id,
                NombreUsuario = usuario.Nombre,
                Email = usuario.Email,
                Alias = usuario.Alias,
                FotoUrl = usuario.FotoUrl,
                ColorPersonalizado = usuario.ColorPersonalizado,
                Token = token,
                TokenExpiration = tokenExpiration,
                Grupo = usuario.Grupo != null ? new GrupoInfo
                {
                    Id = usuario.Grupo.Id,
                    Nombre = usuario.Grupo.Nombre,
                    Descripcion = usuario.Grupo.Descripcion,
                    EsCreador = usuario.Grupo.CreadorId == usuario.Id
                } : null,
                RequiereGrupo = usuario.GrupoId == null,
                EsNuevoUsuario = esNuevo
            };
        }

        private string GenerarColorAleatorio()
        {
            var colores = new[]
            {
                "#E91E63", // Rosa
                "#2196F3", // Azul
                "#4CAF50", // Verde
                "#FF9800", // Naranja
                "#9C27B0", // Púrpura
                "#FF5722", // Rojo
                "#607D8B", // Gris azulado
                "#795548", // Marrón
                "#009688", // Teal
                "#3F51B5", // Índigo
                "#CDDC39", // Lima
                "#FFC107"  // Ámbar
            };

            var random = new Random();
            return colores[random.Next(colores.Length)];
        }

        // Clase auxiliar para Google User
        private class GoogleUser
        {
            public string Id { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string? Picture { get; set; }
        }
    }
}