using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DermaMindProject1.Migrations
{
    /// <inheritdoc />
    public partial class AddSkinTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkinTestQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkinTestQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkinTestResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SkinType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TakenAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkinTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkinTestResults_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SkinTestOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OptionText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    SkinTypePoint = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkinTestOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkinTestOptions_SkinTestQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "SkinTestQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkinTestOptions_QuestionId",
                table: "SkinTestOptions",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkinTestResults_UserId",
                table: "SkinTestResults",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkinTestOptions");

            migrationBuilder.DropTable(
                name: "SkinTestResults");

            migrationBuilder.DropTable(
                name: "SkinTestQuestions");
        }
    }
}
