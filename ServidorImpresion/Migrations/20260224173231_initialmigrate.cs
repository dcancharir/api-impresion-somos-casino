using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ServidorImpresion.Migrations
{
    /// <inheritdoc />
    public partial class initialmigrate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HistorialImpresionCupon",
                columns: table => new
                {
                    HistorialImpresionCuponId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ImpresionCuponId = table.Column<int>(type: "INTEGER", nullable: false),
                    NombreCliente = table.Column<string>(type: "TEXT", nullable: true),
                    NombreImpresora = table.Column<string>(type: "TEXT", nullable: true),
                    NombreSorteo = table.Column<string>(type: "TEXT", nullable: true),
                    NombreSala = table.Column<string>(type: "TEXT", nullable: true),
                    FechaRegistro = table.Column<string>(type: "TEXT", nullable: true),
                    FechaImpresion = table.Column<string>(type: "TEXT", nullable: true),
                    Serie = table.Column<string>(type: "TEXT", nullable: true),
                    CodMaquina = table.Column<string>(type: "TEXT", nullable: true),
                    NroDocumento = table.Column<string>(type: "TEXT", nullable: true),
                    SerieId = table.Column<long>(type: "INTEGER", nullable: false),
                    Impreso = table.Column<int>(type: "INTEGER", nullable: false),
                    Enviado = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialImpresionCupon", x => x.HistorialImpresionCuponId);
                });

            migrationBuilder.CreateTable(
                name: "ImpresionCupon",
                columns: table => new
                {
                    ImpresionCuponId = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    NombreCliente = table.Column<string>(type: "TEXT", nullable: true),
                    NombreImpresora = table.Column<string>(type: "TEXT", nullable: true),
                    NombreSorteo = table.Column<string>(type: "TEXT", nullable: true),
                    NombreSala = table.Column<string>(type: "TEXT", nullable: true),
                    FechaRegistro = table.Column<string>(type: "TEXT", nullable: true),
                    FechaImpresion = table.Column<string>(type: "TEXT", nullable: true),
                    Serie = table.Column<string>(type: "TEXT", nullable: true),
                    CodMaquina = table.Column<string>(type: "TEXT", nullable: true),
                    NroDocumento = table.Column<string>(type: "TEXT", nullable: true),
                    SerieId = table.Column<long>(type: "INTEGER", nullable: false),
                    Impreso = table.Column<int>(type: "INTEGER", nullable: false),
                    Enviado = table.Column<int>(type: "INTEGER", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImpresionCupon", x => x.ImpresionCuponId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HistorialImpresionCupon");

            migrationBuilder.DropTable(
                name: "ImpresionCupon");
        }
    }
}
