using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GastosHogarAPI.Migrations
{
    /// <inheritdoc />
    public partial class AjusteRelacionPlanPagoEstadoMensual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EstadoMensual_PlanPago_PlanPagoId",
                table: "EstadoMensual");

            migrationBuilder.DropIndex(
                name: "IX_EstadoMensual_PlanPagoId",
                table: "EstadoMensual");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoMensual_PlanPagoId",
                table: "EstadoMensual",
                column: "PlanPagoId",
                unique: true,
                filter: "[PlanPagoId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_EstadoMensual_PlanPago_PlanPagoId",
                table: "EstadoMensual",
                column: "PlanPagoId",
                principalTable: "PlanPago",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EstadoMensual_PlanPago_PlanPagoId",
                table: "EstadoMensual");

            migrationBuilder.DropIndex(
                name: "IX_EstadoMensual_PlanPagoId",
                table: "EstadoMensual");

            migrationBuilder.CreateIndex(
                name: "IX_EstadoMensual_PlanPagoId",
                table: "EstadoMensual",
                column: "PlanPagoId");

            migrationBuilder.AddForeignKey(
                name: "FK_EstadoMensual_PlanPago_PlanPagoId",
                table: "EstadoMensual",
                column: "PlanPagoId",
                principalTable: "PlanPago",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
