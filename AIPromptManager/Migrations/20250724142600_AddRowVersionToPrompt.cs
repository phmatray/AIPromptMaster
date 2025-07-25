using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPromptManager.Migrations
{
    /// <inheritdoc />
    public partial class AddRowVersionToPrompt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Prompts",
                type: "BLOB",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Prompts");
        }
    }
}
