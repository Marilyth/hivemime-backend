using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiveMime.Migrations
{
    /// <inheritdoc />
    public partial class AddDatePollTimezoneIgnoreProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IgnoreTimeZone",
                table: "Polls",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IgnoreTimeZone",
                table: "Polls");
        }
    }
}
