using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiveMime.Migrations
{
    /// <inheritdoc />
    public partial class AddDatePollProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "Timestamp",
                table: "CandidateVotes",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "CandidateVotes");
        }
    }
}
