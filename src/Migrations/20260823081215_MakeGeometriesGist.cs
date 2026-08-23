using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiveMime.Migrations
{
    /// <inheritdoc />
    public partial class MakeGeometriesGist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Divisions_Geometry",
                table: "Divisions",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionAreas_Geometry",
                table: "DivisionAreas",
                column: "Geometry")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Divisions_Geometry",
                table: "Divisions");

            migrationBuilder.DropIndex(
                name: "IX_DivisionAreas_Geometry",
                table: "DivisionAreas");
        }
    }
}
