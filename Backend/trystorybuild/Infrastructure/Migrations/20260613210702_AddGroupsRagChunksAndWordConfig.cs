using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupsRagChunksAndWordConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatorId",
                table: "Lessons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatorRole",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsGenerated",
                table: "Lessons",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PromptText",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ImagePrompt",
                table: "LessonPages",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "LessonAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LessonId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetStudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TargetGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LessonAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LessonAssignments_Lessons_LessonId",
                        column: x => x.LessonId,
                        principalTable: "Lessons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LevelWordConfigs",
                columns: table => new
                {
                    Level = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    ExampleSentence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LevelWordConfigs", x => x.Level);
                });

            migrationBuilder.CreateTable(
                name: "RagPageChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceFile = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    Sentence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    ImagePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Letter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LetterName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChromaChunkId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagPageChunks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGroups_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentGroupMembers",
                columns: table => new
                {
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGroupMembers", x => new { x.GroupId, x.StudentId });
                    table.ForeignKey(
                        name: "FK_StudentGroupMembers_StudentGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "StudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentGroupMembers_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LessonAssignments_LessonId",
                table: "LessonAssignments",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroupMembers_StudentId",
                table: "StudentGroupMembers",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_TeacherId",
                table: "StudentGroups",
                column: "TeacherId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LessonAssignments");

            migrationBuilder.DropTable(
                name: "LevelWordConfigs");

            migrationBuilder.DropTable(
                name: "RagPageChunks");

            migrationBuilder.DropTable(
                name: "StudentGroupMembers");

            migrationBuilder.DropTable(
                name: "StudentGroups");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "CreatorRole",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "IsGenerated",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "PromptText",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "ImagePrompt",
                table: "LessonPages");
        }
    }
}
