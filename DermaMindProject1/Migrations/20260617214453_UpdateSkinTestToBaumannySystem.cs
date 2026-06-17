using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DermaMindProject1.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSkinTestToBaumannySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkinType",
                table: "SkinTestResults");

            migrationBuilder.DropColumn(
                name: "SkinTypePoint",
                table: "SkinTestOptions");

            migrationBuilder.AddColumn<int>(
                name: "OD_Score",
                table: "SkinTestResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PN_Score",
                table: "SkinTestResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SR_Score",
                table: "SkinTestResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SkinTypeCode",
                table: "SkinTestResults",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WT_Score",
                table: "SkinTestResults",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "SkinTestQuestions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Score",
                table: "SkinTestOptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SkinTypeProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Strategy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkinTypeProfiles", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkinTypeProfiles");

            migrationBuilder.DropColumn(
                name: "OD_Score",
                table: "SkinTestResults");

            migrationBuilder.DropColumn(
                name: "PN_Score",
                table: "SkinTestResults");

            migrationBuilder.DropColumn(
                name: "SR_Score",
                table: "SkinTestResults");

            migrationBuilder.DropColumn(
                name: "SkinTypeCode",
                table: "SkinTestResults");

            migrationBuilder.DropColumn(
                name: "WT_Score",
                table: "SkinTestResults");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "SkinTestQuestions");

            migrationBuilder.DropColumn(
                name: "Score",
                table: "SkinTestOptions");

            migrationBuilder.AddColumn<string>(
                name: "SkinType",
                table: "SkinTestResults",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SkinTypePoint",
                table: "SkinTestOptions",
                type: "text",
                nullable: true);
        }
    }
}
