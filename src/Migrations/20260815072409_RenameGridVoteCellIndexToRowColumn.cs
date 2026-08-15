using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiveMime.Migrations
{
    /// <inheritdoc />
    public partial class RenameGridVoteCellIndexToRowColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateVotes_CellIndex",
                table: "CandidateVotes");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "CandidateVotes");

            migrationBuilder.RenameColumn(
                name: "CellIndex",
                table: "CandidateVotes",
                newName: "Row");

            migrationBuilder.AddColumn<int>(
                name: "Column",
                table: "CandidateVotes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateVotes_Row_Column",
                table: "CandidateVotes",
                columns: new[] { "Row", "Column" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CandidateVotes_Row_Column",
                table: "CandidateVotes");

            migrationBuilder.DropColumn(
                name: "Column",
                table: "CandidateVotes");

            migrationBuilder.RenameColumn(
                name: "Row",
                table: "CandidateVotes",
                newName: "CellIndex");

            migrationBuilder.AddColumn<double>(
                name: "Value",
                table: "CandidateVotes",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateVotes_CellIndex",
                table: "CandidateVotes",
                column: "CellIndex");
        }
    }
}
