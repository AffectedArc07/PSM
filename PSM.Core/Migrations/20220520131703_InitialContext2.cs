using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSM.Core.Migrations
{
    public partial class InitialContext2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "CreatedById",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedById",
                table: "Users",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Users_CreatedById",
                table: "Users",
                column: "CreatedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Users_CreatedById",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_CreatedById",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Users");

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
