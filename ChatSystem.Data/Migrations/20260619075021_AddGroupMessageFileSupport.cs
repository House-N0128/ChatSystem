using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChatSystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupMessageFileSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "GroupMessages",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FilePath",
                table: "GroupMessages",
                type: "varchar(512)",
                maxLength: 512,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "GroupMessages");

            migrationBuilder.DropColumn(
                name: "FilePath",
                table: "GroupMessages");
        }
    }
}
