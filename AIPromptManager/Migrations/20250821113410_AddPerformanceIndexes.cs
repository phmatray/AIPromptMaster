using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIPromptManager.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_PromptTags_PromptId",
                table: "PromptTags",
                column: "PromptId");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_CreatedAt",
                table: "Prompts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_Title",
                table: "Prompts",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_Prompts_UpdatedAt",
                table: "Prompts",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PromptTags_PromptId",
                table: "PromptTags");

            migrationBuilder.DropIndex(
                name: "IX_Prompts_CreatedAt",
                table: "Prompts");

            migrationBuilder.DropIndex(
                name: "IX_Prompts_Title",
                table: "Prompts");

            migrationBuilder.DropIndex(
                name: "IX_Prompts_UpdatedAt",
                table: "Prompts");
        }
    }
}
