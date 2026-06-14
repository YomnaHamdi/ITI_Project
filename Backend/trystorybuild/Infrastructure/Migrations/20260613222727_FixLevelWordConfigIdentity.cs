using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixLevelWordConfigIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server cannot ALTER an IDENTITY column — drop and recreate the table.
            // The table is empty at this point (all inserts were failing due to IDENTITY mismatch).
            migrationBuilder.Sql("DROP TABLE IF EXISTS [LevelWordConfigs];");

            migrationBuilder.Sql(@"
                CREATE TABLE [LevelWordConfigs] (
                    [Level]           INT           NOT NULL,
                    [WordCount]       INT           NOT NULL,
                    [ExampleSentence] NVARCHAR(MAX) NOT NULL,
                    [UpdatedAt]       DATETIME2     NOT NULL,
                    CONSTRAINT [PK_LevelWordConfigs] PRIMARY KEY ([Level])
                );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS [LevelWordConfigs];");

            migrationBuilder.Sql(@"
                CREATE TABLE [LevelWordConfigs] (
                    [Level]           INT IDENTITY(1,1) NOT NULL,
                    [WordCount]       INT               NOT NULL,
                    [ExampleSentence] NVARCHAR(MAX)     NOT NULL,
                    [UpdatedAt]       DATETIME2         NOT NULL,
                    CONSTRAINT [PK_LevelWordConfigs] PRIMARY KEY ([Level])
                );");
        }
    }
}
