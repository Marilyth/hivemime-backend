using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace HiveMime.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .Annotation("Npgsql:PostgresExtension:postgis", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "Divisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LocalName = table.Column<string>(type: "citext", maxLength: 1000, nullable: false),
                    EnglishName = table.Column<string>(type: "citext", maxLength: 1000, nullable: true),
                    Country = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Region = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AdminLevel = table.Column<int>(type: "integer", nullable: true),
                    Population = table.Column<int>(type: "integer", nullable: true),
                    Subtype = table.Column<int>(type: "integer", nullable: false),
                    Class = table.Column<int>(type: "integer", nullable: true),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Geometry = table.Column<Geometry>(type: "geometry", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Divisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Divisions_Divisions_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Divisions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DivisionAreas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaClass = table.Column<int>(type: "integer", nullable: false),
                    DivisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Geometry = table.Column<Geometry>(type: "geometry", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DivisionAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DivisionAreas_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DivisionDivision",
                columns: table => new
                {
                    CapitalOfId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapitalsId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DivisionDivision", x => new { x.CapitalOfId, x.CapitalsId });
                    table.ForeignKey(
                        name: "FK_DivisionDivision_Divisions_CapitalOfId",
                        column: x => x.CapitalOfId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DivisionDivision_Divisions_CapitalsId",
                        column: x => x.CapitalsId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DivisionAreas_DivisionId",
                table: "DivisionAreas",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionDivision_CapitalsId",
                table: "DivisionDivision",
                column: "CapitalsId");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_EnglishName",
                table: "Divisions",
                column: "EnglishName");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_LocalName",
                table: "Divisions",
                column: "LocalName");

            migrationBuilder.CreateIndex(
                name: "IX_Divisions_ParentId",
                table: "Divisions",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DivisionAreas");

            migrationBuilder.DropTable(
                name: "DivisionDivision");

            migrationBuilder.DropTable(
                name: "Divisions");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
