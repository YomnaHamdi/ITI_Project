using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLessonProgressTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentProgress_Stories_StoryId",
                table: "StudentProgress");

            migrationBuilder.DropIndex(
                name: "IX_StudentProgress_StoryId_ChildName",
                table: "StudentProgress");

            migrationBuilder.AlterColumn<Guid>(
                name: "StoryId",
                table: "StudentProgress",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "LessonId",
                table: "StudentProgress",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentProgress_LessonId",
                table: "StudentProgress",
                column: "LessonId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentProgress_StoryId",
                table: "StudentProgress",
                column: "StoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProgress_Lessons_LessonId",
                table: "StudentProgress",
                column: "LessonId",
                principalTable: "Lessons",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProgress_Stories_StoryId",
                table: "StudentProgress",
                column: "StoryId",
                principalTable: "Stories",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentProgress_Lessons_LessonId",
                table: "StudentProgress");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentProgress_Stories_StoryId",
                table: "StudentProgress");

            migrationBuilder.DropIndex(
                name: "IX_StudentProgress_LessonId",
                table: "StudentProgress");

            migrationBuilder.DropIndex(
                name: "IX_StudentProgress_StoryId",
                table: "StudentProgress");

            migrationBuilder.DropColumn(
                name: "LessonId",
                table: "StudentProgress");

            migrationBuilder.AlterColumn<Guid>(
                name: "StoryId",
                table: "StudentProgress",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentProgress_StoryId_ChildName",
                table: "StudentProgress",
                columns: new[] { "StoryId", "ChildName" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentProgress_Stories_StoryId",
                table: "StudentProgress",
                column: "StoryId",
                principalTable: "Stories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
