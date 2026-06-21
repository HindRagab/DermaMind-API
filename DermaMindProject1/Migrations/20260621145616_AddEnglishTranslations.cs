using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DermaMindProject1.Migrations
{
    /// <inheritdoc />
    public partial class AddEnglishTranslations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "SkinTypeProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrategyEn",
                table: "SkinTypeProfiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuestionTextEn",
                table: "SkinTestQuestions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OptionTextEn",
                table: "SkinTestOptions",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "SkinTypeProfiles");

            migrationBuilder.DropColumn(
                name: "StrategyEn",
                table: "SkinTypeProfiles");

            migrationBuilder.DropColumn(
                name: "QuestionTextEn",
                table: "SkinTestQuestions");

            migrationBuilder.DropColumn(
                name: "OptionTextEn",
                table: "SkinTestOptions");
        }
    }
}
