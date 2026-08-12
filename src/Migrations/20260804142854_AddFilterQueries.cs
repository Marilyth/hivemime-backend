using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiveMime.Migrations
{
    /// <inheritdoc />
    public partial class AddFilterQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<FilterQueryBase>(
                name: "ConditionQuery",
                table: "Polls",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<FilterQueryBase>(
                name: "DateFilterQuery",
                table: "Polls",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConditionQuery",
                table: "Polls");

            migrationBuilder.DropColumn(
                name: "DateFilterQuery",
                table: "Polls");
        }
    }
}
