using GastosHogarAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GastosHogarAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets principales
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<Gasto> Gastos { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<EstadoMensual> EstadoMensual { get; set; } // Mantener nombre singular
        public DbSet<PlanPago> PlanesPago { get; set; }
        public DbSet<CuotaPago> CuotasPago { get; set; }
        public DbSet<DispositivoUsuario> DispositivosUsuario { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigurarUsuario(modelBuilder);
            ConfigurarGrupo(modelBuilder);
            ConfigurarCategoria(modelBuilder);
            ConfigurarGasto(modelBuilder);
            ConfigurarDispositivoUsuario(modelBuilder);
            ConfigurarEstadoMensual(modelBuilder);
            ConfigurarPlanPago(modelBuilder);
            ConfigurarCuotaPago(modelBuilder);

            // Insertar datos iniciales
            SeedCategoriasPredeterminadas(modelBuilder);
        }

        private void ConfigurarUsuario(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuración de propiedades
                entity.Property(e => e.Nombre)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Email)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.Alias)
                    .HasMaxLength(50);

                entity.Property(e => e.Pin)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(e => e.GoogleId)
                    .HasMaxLength(100);

                entity.Property(e => e.FotoUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.ColorPersonalizado)
                    .HasMaxLength(7)
                    .HasDefaultValue("#2196F3");

                // Propiedades decimales
                entity.Property(e => e.TotalGastado)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0m);

                entity.Property(e => e.TotalPagado)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0m);

                // Propiedades enteras
                entity.Property(e => e.GastosCreados)
                    .HasDefaultValue(0);

                // Propiedades de fecha
                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()"); // UTC para mejor manejo de zonas horarias

                entity.Property(e => e.UltimoAcceso)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Propiedades booleanas
                entity.Property(e => e.Activo)
                    .HasDefaultValue(true);

                // Índices
                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("IX_Usuario_Email");

                entity.HasIndex(e => e.GoogleId)
                    .HasDatabaseName("IX_Usuario_GoogleId")
                    .HasFilter("[GoogleId] IS NOT NULL");

                // Relaciones
                entity.HasOne(e => e.Grupo)
                    .WithMany(g => g.Miembros)
                    .HasForeignKey(e => e.GrupoId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigurarGrupo(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Grupo>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuración de propiedades
                entity.Property(e => e.Nombre)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.CodigoInvitacion)
                    .HasMaxLength(6)
                    .IsRequired();

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(500);

                // Propiedades decimales
                entity.Property(e => e.TotalGastado)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0m);

                // Propiedades enteras
                entity.Property(e => e.TotalLiquidaciones)
                    .HasDefaultValue(0);

                // Propiedades de fecha
                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Propiedades booleanas
                entity.Property(e => e.Activo)
                    .HasDefaultValue(true);

                // Índices
                entity.HasIndex(e => e.CodigoInvitacion)
                    .IsUnique()
                    .HasDatabaseName("IX_Grupo_CodigoInvitacion");

                entity.HasIndex(e => e.CreadorId)
                    .HasDatabaseName("IX_Grupo_CreadorId");

                // Relaciones
                entity.HasOne(e => e.Creador)
                    .WithMany()
                    .HasForeignKey(e => e.CreadorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurarCategoria(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuración de propiedades
                entity.Property(e => e.Nombre)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.Emoji)
                    .HasMaxLength(10)
                    .HasDefaultValue("📝");

                entity.Property(e => e.Color)
                    .HasMaxLength(7)
                    .HasDefaultValue("#757575");

                // Propiedades booleanas
                entity.Property(e => e.EsPredeterminada)
                    .HasDefaultValue(false);

                entity.Property(e => e.Activa)
                    .HasDefaultValue(true);

                // Propiedades enteras
                entity.Property(e => e.VecesUsada)
                    .HasDefaultValue(0);

                // Propiedades decimales
                entity.Property(e => e.TotalGastado)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0m);

                // Propiedades de fecha
                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Índices
                entity.HasIndex(e => new { e.Nombre, e.GrupoId })
                    .IsUnique()
                    .HasDatabaseName("IX_Categoria_Nombre_GrupoId");

                entity.HasIndex(e => e.EsPredeterminada)
                    .HasDatabaseName("IX_Categoria_EsPredeterminada");

                // Relaciones
                entity.HasOne(e => e.Grupo)
                    .WithMany()
                    .HasForeignKey(e => e.GrupoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigurarGasto(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Gasto>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuración de propiedades
                entity.Property(e => e.Importe)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.Descripcion)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.Tienda)
                    .HasMaxLength(200);

                entity.Property(e => e.Notas)
                    .HasMaxLength(500);

                entity.Property(e => e.NombreFotoTicket)
                    .HasMaxLength(255);

                // Propiedades booleanas
                entity.Property(e => e.EsDetalle)
                    .HasDefaultValue(false);

                // Propiedades de fecha
                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Índices para optimizar consultas
                entity.HasIndex(e => new { e.GrupoId, e.Mes, e.Año })
                    .HasDatabaseName("IX_Gasto_Grupo_Mes_Año");

                entity.HasIndex(e => new { e.UsuarioId, e.Fecha })
                    .HasDatabaseName("IX_Gasto_Usuario_Fecha");

                entity.HasIndex(e => e.CategoriaId)
                    .HasDatabaseName("IX_Gasto_CategoriaId");

                // Relaciones
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Gastos)
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Grupo)
                    .WithMany(g => g.Gastos)
                    .HasForeignKey(e => e.GrupoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Categoria)
                    .WithMany()
                    .HasForeignKey(e => e.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurarDispositivoUsuario(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DispositivoUsuario>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuración de propiedades
                entity.Property(e => e.DeviceId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.NombreDispositivo)
                    .HasMaxLength(100);

                entity.Property(e => e.TipoDispositivo)
                    .HasMaxLength(50);

                entity.Property(e => e.VersionSO)
                    .HasMaxLength(20);

                // Propiedades de fecha
                entity.Property(e => e.FechaVinculacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.UltimoAcceso)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Propiedades booleanas
                entity.Property(e => e.Activo)
                    .HasDefaultValue(true);

                // Índices
                entity.HasIndex(e => new { e.UsuarioId, e.DeviceId })
                    .IsUnique()
                    .HasDatabaseName("IX_DispositivoUsuario_Usuario_Device");

                // Relaciones
                entity.HasOne(e => e.Usuario)
                    .WithMany(u => u.Dispositivos)
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigurarEstadoMensual(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<EstadoMensual>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Configuración de propiedades
                entity.Property(e => e.GrupoId)
                    .IsRequired();

                // Propiedades decimales
                entity.Property(e => e.MontoDeuda)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0m);

                // Propiedades de texto
                entity.Property(e => e.EstadoPago)
                    .HasMaxLength(20)
                    .HasDefaultValue("pendiente");

                entity.Property(e => e.MetodoPago)
                    .HasMaxLength(50);

                // Configuración JSON mejorada
                entity.Property(e => e.ConfirmacionesUsuarios)
                    .HasColumnType("nvarchar(max)")
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                        v => JsonSerializer.Deserialize<Dictionary<int, bool>>(v, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? new Dictionary<int, bool>()
                    );

                // Índices únicos y de consulta
                entity.HasIndex(e => new { e.GrupoId, e.Mes, e.Año })
                    .IsUnique()
                    .HasDatabaseName("IX_EstadoMensual_Grupo_Mes_Año");

                entity.HasIndex(e => new { e.GrupoId, e.EstadoPago })
                    .HasDatabaseName("IX_EstadoMensual_Grupo_Estado");

                entity.HasIndex(e => new { e.DeudorId, e.AcreedorId })
                    .HasDatabaseName("IX_EstadoMensual_Deudor_Acreedor");

                // Relaciones
                entity.HasOne(e => e.Grupo)
                    .WithMany()
                    .HasForeignKey(e => e.GrupoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Deudor)
                    .WithMany()
                    .HasForeignKey(e => e.DeudorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Acreedor)
                    .WithMany()
                    .HasForeignKey(e => e.AcreedorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.PlanPago)
                    .WithOne()
                    .HasForeignKey<EstadoMensual>(e => e.PlanPagoId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurarPlanPago(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PlanPago>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Propiedades decimales
                entity.Property(e => e.MontoTotal)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                entity.Property(e => e.MontoCuota)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.MontoPagado)
                    .HasColumnType("decimal(18,2)")
                    .HasDefaultValue(0m);

                entity.Property(e => e.MontoRestante)
                    .HasColumnType("decimal(18,2)");

                // Propiedades de texto
                entity.Property(e => e.Descripcion)
                    .HasMaxLength(500);

                entity.Property(e => e.Motivo)
                    .HasMaxLength(200);

                // Propiedades de fecha
                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Propiedades booleanas
                entity.Property(e => e.Activo)
                    .HasDefaultValue(true);

                entity.Property(e => e.Completado)
                    .HasDefaultValue(false);

                // Propiedades enteras
                entity.Property(e => e.CuotasPagadas)
                    .HasDefaultValue(0);

                // Índices para optimizar consultas
                entity.HasIndex(e => new { e.GrupoId, e.Activo })
                    .HasDatabaseName("IX_PlanPago_Grupo_Activo");

                entity.HasIndex(e => new { e.DeudorId, e.Completado })
                    .HasDatabaseName("IX_PlanPago_Deudor_Completado");

                entity.HasIndex(e => new { e.AcreedorId, e.Completado })
                    .HasDatabaseName("IX_PlanPago_Acreedor_Completado");

                // Relaciones sin navigation properties problemáticas
                entity.HasOne<Grupo>()
                    .WithMany()
                    .HasForeignKey(e => e.GrupoId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(e => e.DeudorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(e => e.AcreedorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigurarCuotaPago(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CuotaPago>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Propiedades decimales
                entity.Property(e => e.Monto)
                    .HasColumnType("decimal(18,2)")
                    .IsRequired();

                // Propiedades de texto
                entity.Property(e => e.MetodoPago)
                    .HasMaxLength(50);

                entity.Property(e => e.NotasPago)
                    .HasMaxLength(500);

                // Propiedades enum
                entity.Property(e => e.Estado)
                    .HasConversion<string>()
                    .HasDefaultValue(EstadoCuota.Pendiente);

                // Propiedades de fecha
                entity.Property(e => e.FechaCreacion)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Propiedades booleanas
                entity.Property(e => e.Confirmado)
                    .HasDefaultValue(false);

                // Índices
                entity.HasIndex(e => new { e.PlanPagoId, e.NumeroCuota })
                    .IsUnique()
                    .HasDatabaseName("IX_CuotaPago_Plan_Numero");

                entity.HasIndex(e => new { e.Estado, e.FechaVencimiento })
                    .HasDatabaseName("IX_CuotaPago_Estado_Vencimiento");

                // Relaciones
                entity.HasOne(e => e.PlanPago)
                    .WithMany(p => p.Cuotas)
                    .HasForeignKey(e => e.PlanPagoId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void SeedCategoriasPredeterminadas(ModelBuilder modelBuilder)
        {
            var fechaBase = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var categoriasPredeterminadas = new[]
            {
                new Categoria { Id = 1, Nombre = "Alimentación", Emoji = "🛒", Color = "#4CAF50", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 2, Nombre = "Transporte", Emoji = "🚗", Color = "#2196F3", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 3, Nombre = "Servicios", Emoji = "💡", Color = "#FF9800", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 4, Nombre = "Entretenimiento", Emoji = "🎬", Color = "#E91E63", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 5, Nombre = "Salud", Emoji = "⚕️", Color = "#009688", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 6, Nombre = "Hogar", Emoji = "🏠", Color = "#795548", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 7, Nombre = "Ropa", Emoji = "👕", Color = "#9C27B0", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 8, Nombre = "Educación", Emoji = "📚", Color = "#3F51B5", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 9, Nombre = "Tecnología", Emoji = "💻", Color = "#607D8B", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 10, Nombre = "Regalos", Emoji = "🎁", Color = "#FF5722", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 11, Nombre = "Viajes", Emoji = "✈️", Color = "#CDDC39", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m },
                new Categoria { Id = 12, Nombre = "Otros", Emoji = "📝", Color = "#757575", EsPredeterminada = true, Activa = true, FechaCreacion = fechaBase, VecesUsada = 0, TotalGastado = 0m }
            };

            modelBuilder.Entity<Categoria>().HasData(categoriasPredeterminadas);
        }
    }
}