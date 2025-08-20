using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GastosHogarAPI.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuotaPago_PlanPago_PlanPagoId",
                table: "CuotaPago");

            migrationBuilder.DropForeignKey(
                name: "FK_EstadoMensual_PlanPago_PlanPagoId",
                table: "EstadoMensual");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanPago_Grupos_GrupoId",
                table: "PlanPago");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanPago_Usuarios_AcreedorId",
                table: "PlanPago");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanPago_Usuarios_DeudorId",
                table: "PlanPago");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_GoogleId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_EstadoMensual_DeudorId",
                table: "EstadoMensual");

            migrationBuilder.DropIndex(
                name: "IX_DispositivosUsuario_DeviceId",
                table: "DispositivosUsuario");

            migrationBuilder.DropIndex(
                name: "IX_DispositivosUsuario_UsuarioId",
                table: "DispositivosUsuario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlanPago",
                table: "PlanPago");

            migrationBuilder.DropIndex(
                name: "IX_PlanPago_AcreedorId_Activo",
                table: "PlanPago");

            migrationBuilder.DropIndex(
                name: "IX_PlanPago_EstadoMensualId",
                table: "PlanPago");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CuotaPago",
                table: "CuotaPago");

            migrationBuilder.DropIndex(
                name: "IX_CuotaPago_FechaPago",
                table: "CuotaPago");

            migrationBuilder.DropColumn(
                name: "FechaCreacion",
                table: "EstadoMensual");

            migrationBuilder.RenameTable(
                name: "PlanPago",
                newName: "PlanesPago");

            migrationBuilder.RenameTable(
                name: "CuotaPago",
                newName: "CuotasPago");

            migrationBuilder.RenameIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                newName: "IX_Usuario_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Grupos_CreadorId",
                table: "Grupos",
                newName: "IX_Grupo_CreadorId");

            migrationBuilder.RenameIndex(
                name: "IX_Grupos_CodigoInvitacion",
                table: "Grupos",
                newName: "IX_Grupo_CodigoInvitacion");

            migrationBuilder.RenameIndex(
                name: "IX_Gastos_UsuarioId_Fecha",
                table: "Gastos",
                newName: "IX_Gasto_Usuario_Fecha");

            migrationBuilder.RenameIndex(
                name: "IX_Gastos_GrupoId_Mes_Año",
                table: "Gastos",
                newName: "IX_Gasto_Grupo_Mes_Año");

            migrationBuilder.RenameIndex(
                name: "IX_Gastos_CategoriaId",
                table: "Gastos",
                newName: "IX_Gasto_CategoriaId");

            migrationBuilder.RenameIndex(
                name: "IX_EstadoMensual_GrupoId_Mes_Año",
                table: "EstadoMensual",
                newName: "IX_EstadoMensual_Grupo_Mes_Año");

            migrationBuilder.RenameIndex(
                name: "IX_EstadoMensual_GrupoId_EstadoPago",
                table: "EstadoMensual",
                newName: "IX_EstadoMensual_Grupo_Estado");

            migrationBuilder.RenameIndex(
                name: "IX_PlanPago_GrupoId_Activo",
                table: "PlanesPago",
                newName: "IX_PlanPago_Grupo_Activo");

            migrationBuilder.RenameIndex(
                name: "IX_PlanPago_DeudorId_Completado",
                table: "PlanesPago",
                newName: "IX_PlanPago_Deudor_Completado");

            migrationBuilder.RenameIndex(
                name: "IX_CuotaPago_PlanPagoId_NumeroCuota",
                table: "CuotasPago",
                newName: "IX_CuotaPago_Plan_Numero");

            migrationBuilder.RenameIndex(
                name: "IX_CuotaPago_Estado_FechaVencimiento",
                table: "CuotasPago",
                newName: "IX_CuotaPago_Estado_Vencimiento");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UltimoAcceso",
                table: "Usuarios",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Pin",
                table: "Usuarios",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "GoogleId",
                table: "Usuarios",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "GastosCreados",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "FotoUrl",
                table: "Usuarios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "Usuarios",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "Activo",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "TotalLiquidaciones",
                table: "Grupos",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "Grupos",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "Activo",
                table: "Grupos",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<decimal>(
                name: "Importe",
                table: "Gastos",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "Gastos",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "EsDetalle",
                table: "Gastos",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoDeuda",
                table: "EstadoMensual",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UltimoAcceso",
                table: "DispositivosUsuario",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaVinculacion",
                table: "DispositivosUsuario",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "Activo",
                table: "DispositivosUsuario",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<int>(
                name: "VecesUsada",
                table: "Categorias",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "Categorias",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<bool>(
                name: "EsPredeterminada",
                table: "Categorias",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "Activa",
                table: "Categorias",
                type: "bit",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoTotal",
                table: "PlanesPago",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoRestante",
                table: "PlanesPago",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoPagado",
                table: "PlanesPago",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoCuota",
                table: "PlanesPago",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "PlanesPago",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<decimal>(
                name: "Monto",
                table: "CuotasPago",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "CuotasPago",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "CuotasPago",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "Pendiente",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Pendiente");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlanesPago",
                table: "PlanesPago",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CuotasPago",
                table: "CuotasPago",
                column: "Id");

            migrationBuilder.InsertData(
                table: "Categorias",
                columns: new[] { "Id", "Activa", "Color", "Emoji", "EsPredeterminada", "FechaCreacion", "GrupoId", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "#4CAF50", "🛒", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Alimentación" },
                    { 2, true, "#2196F3", "🚗", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Transporte" },
                    { 3, true, "#FF9800", "💡", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Servicios" },
                    { 4, true, "#E91E63", "🎬", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Entretenimiento" },
                    { 5, true, "#009688", "⚕️", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Salud" },
                    { 6, true, "#795548", "🏠", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Hogar" },
                    { 7, true, "#9C27B0", "👕", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ropa" },
                    { 8, true, "#3F51B5", "📚", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Educación" },
                    { 9, true, "#607D8B", "💻", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Tecnología" },
                    { 10, true, "#FF5722", "🎁", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Regalos" },
                    { 11, true, "#CDDC39", "✈️", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Viajes" },
                    { 12, true, "#757575", "📝", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Otros" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_GoogleId",
                table: "Usuarios",
                column: "GoogleId",
                filter: "[GoogleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoMensual_Deudor_Acreedor",
                table: "EstadoMensual",
                columns: new[] { "DeudorId", "AcreedorId" });

            migrationBuilder.CreateIndex(
                name: "IX_DispositivoUsuario_Usuario_Device",
                table: "DispositivosUsuario",
                columns: new[] { "UsuarioId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Categoria_EsPredeterminada",
                table: "Categorias",
                column: "EsPredeterminada");

            migrationBuilder.CreateIndex(
                name: "IX_Categoria_Nombre_GrupoId",
                table: "Categorias",
                columns: new[] { "Nombre", "GrupoId" },
                unique: true,
                filter: "[GrupoId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPago_Acreedor_Completado",
                table: "PlanesPago",
                columns: new[] { "AcreedorId", "Completado" });

            migrationBuilder.AddForeignKey(
                name: "FK_CuotasPago_PlanesPago_PlanPagoId",
                table: "CuotasPago",
                column: "PlanPagoId",
                principalTable: "PlanesPago",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EstadoMensual_PlanesPago_PlanPagoId",
                table: "EstadoMensual",
                column: "PlanPagoId",
                principalTable: "PlanesPago",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanesPago_Grupos_GrupoId",
                table: "PlanesPago",
                column: "GrupoId",
                principalTable: "Grupos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanesPago_Usuarios_AcreedorId",
                table: "PlanesPago",
                column: "AcreedorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanesPago_Usuarios_DeudorId",
                table: "PlanesPago",
                column: "DeudorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CuotasPago_PlanesPago_PlanPagoId",
                table: "CuotasPago");

            migrationBuilder.DropForeignKey(
                name: "FK_EstadoMensual_PlanesPago_PlanPagoId",
                table: "EstadoMensual");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanesPago_Grupos_GrupoId",
                table: "PlanesPago");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanesPago_Usuarios_AcreedorId",
                table: "PlanesPago");

            migrationBuilder.DropForeignKey(
                name: "FK_PlanesPago_Usuarios_DeudorId",
                table: "PlanesPago");

            migrationBuilder.DropIndex(
                name: "IX_Usuario_GoogleId",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_EstadoMensual_Deudor_Acreedor",
                table: "EstadoMensual");

            migrationBuilder.DropIndex(
                name: "IX_DispositivoUsuario_Usuario_Device",
                table: "DispositivosUsuario");

            migrationBuilder.DropIndex(
                name: "IX_Categoria_EsPredeterminada",
                table: "Categorias");

            migrationBuilder.DropIndex(
                name: "IX_Categoria_Nombre_GrupoId",
                table: "Categorias");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PlanesPago",
                table: "PlanesPago");

            migrationBuilder.DropIndex(
                name: "IX_PlanPago_Acreedor_Completado",
                table: "PlanesPago");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CuotasPago",
                table: "CuotasPago");

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.RenameTable(
                name: "PlanesPago",
                newName: "PlanPago");

            migrationBuilder.RenameTable(
                name: "CuotasPago",
                newName: "CuotaPago");

            migrationBuilder.RenameIndex(
                name: "IX_Usuario_Email",
                table: "Usuarios",
                newName: "IX_Usuarios_Email");

            migrationBuilder.RenameIndex(
                name: "IX_Grupo_CreadorId",
                table: "Grupos",
                newName: "IX_Grupos_CreadorId");

            migrationBuilder.RenameIndex(
                name: "IX_Grupo_CodigoInvitacion",
                table: "Grupos",
                newName: "IX_Grupos_CodigoInvitacion");

            migrationBuilder.RenameIndex(
                name: "IX_Gasto_Usuario_Fecha",
                table: "Gastos",
                newName: "IX_Gastos_UsuarioId_Fecha");

            migrationBuilder.RenameIndex(
                name: "IX_Gasto_Grupo_Mes_Año",
                table: "Gastos",
                newName: "IX_Gastos_GrupoId_Mes_Año");

            migrationBuilder.RenameIndex(
                name: "IX_Gasto_CategoriaId",
                table: "Gastos",
                newName: "IX_Gastos_CategoriaId");

            migrationBuilder.RenameIndex(
                name: "IX_EstadoMensual_Grupo_Mes_Año",
                table: "EstadoMensual",
                newName: "IX_EstadoMensual_GrupoId_Mes_Año");

            migrationBuilder.RenameIndex(
                name: "IX_EstadoMensual_Grupo_Estado",
                table: "EstadoMensual",
                newName: "IX_EstadoMensual_GrupoId_EstadoPago");

            migrationBuilder.RenameIndex(
                name: "IX_PlanPago_Grupo_Activo",
                table: "PlanPago",
                newName: "IX_PlanPago_GrupoId_Activo");

            migrationBuilder.RenameIndex(
                name: "IX_PlanPago_Deudor_Completado",
                table: "PlanPago",
                newName: "IX_PlanPago_DeudorId_Completado");

            migrationBuilder.RenameIndex(
                name: "IX_CuotaPago_Plan_Numero",
                table: "CuotaPago",
                newName: "IX_CuotaPago_PlanPagoId_NumeroCuota");

            migrationBuilder.RenameIndex(
                name: "IX_CuotaPago_Estado_Vencimiento",
                table: "CuotaPago",
                newName: "IX_CuotaPago_Estado_FechaVencimiento");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UltimoAcceso",
                table: "Usuarios",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Pin",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "GoogleId",
                table: "Usuarios",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "GastosCreados",
                table: "Usuarios",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "FotoUrl",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "Usuarios",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<bool>(
                name: "Activo",
                table: "Usuarios",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "TotalLiquidaciones",
                table: "Grupos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "Grupos",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<bool>(
                name: "Activo",
                table: "Grupos",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Importe",
                table: "Gastos",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "Gastos",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<bool>(
                name: "EsDetalle",
                table: "Gastos",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoDeuda",
                table: "EstadoMensual",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaCreacion",
                table: "EstadoMensual",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UltimoAcceso",
                table: "DispositivosUsuario",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaVinculacion",
                table: "DispositivosUsuario",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<bool>(
                name: "Activo",
                table: "DispositivosUsuario",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "VecesUsada",
                table: "Categorias",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "Categorias",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<bool>(
                name: "EsPredeterminada",
                table: "Categorias",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "Activa",
                table: "Categorias",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoTotal",
                table: "PlanPago",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoRestante",
                table: "PlanPago",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoPagado",
                table: "PlanPago",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "MontoCuota",
                table: "PlanPago",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "PlanPago",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<decimal>(
                name: "Monto",
                table: "CuotaPago",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "CuotaPago",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "CuotaPago",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pendiente",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldDefaultValue: "Pendiente");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PlanPago",
                table: "PlanPago",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CuotaPago",
                table: "CuotaPago",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_GoogleId",
                table: "Usuarios",
                column: "GoogleId",
                unique: true,
                filter: "[GoogleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoMensual_DeudorId",
                table: "EstadoMensual",
                column: "DeudorId");

            migrationBuilder.CreateIndex(
                name: "IX_DispositivosUsuario_DeviceId",
                table: "DispositivosUsuario",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispositivosUsuario_UsuarioId",
                table: "DispositivosUsuario",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanPago_AcreedorId_Activo",
                table: "PlanPago",
                columns: new[] { "AcreedorId", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_PlanPago_EstadoMensualId",
                table: "PlanPago",
                column: "EstadoMensualId");

            migrationBuilder.CreateIndex(
                name: "IX_CuotaPago_FechaPago",
                table: "CuotaPago",
                column: "FechaPago");

            migrationBuilder.AddForeignKey(
                name: "FK_CuotaPago_PlanPago_PlanPagoId",
                table: "CuotaPago",
                column: "PlanPagoId",
                principalTable: "PlanPago",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EstadoMensual_PlanPago_PlanPagoId",
                table: "EstadoMensual",
                column: "PlanPagoId",
                principalTable: "PlanPago",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PlanPago_Grupos_GrupoId",
                table: "PlanPago",
                column: "GrupoId",
                principalTable: "Grupos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanPago_Usuarios_AcreedorId",
                table: "PlanPago",
                column: "AcreedorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PlanPago_Usuarios_DeudorId",
                table: "PlanPago",
                column: "DeudorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
