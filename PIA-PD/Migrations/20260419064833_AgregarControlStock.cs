using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PIA_PD.Migrations
{
    /// <inheritdoc />
    public partial class AgregarControlStock : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PortadaUrl",
                table: "LibrosInternos",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "LibrosInternos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "LibrosInternos");

            migrationBuilder.UpdateData(
                table: "LibrosInternos",
                keyColumn: "PortadaUrl",
                keyValue: null,
                column: "PortadaUrl",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "PortadaUrl",
                table: "LibrosInternos",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
