using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSM.Core.Migrations
{
    public partial class instance_refactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Instances",
                table: "Instances");

            migrationBuilder.RenameTable(
                name: "Instances",
                newName: "instances");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "instances",
                newName: "enabled");

            migrationBuilder.RenameColumn(
                name: "instance_path",
                table: "instances",
                newName: "root");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "instances",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "instances",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .Annotation("Relational:ColumnOrder", 0)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_instances",
                table: "instances",
                column: "id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_instances",
                table: "instances");

            migrationBuilder.RenameTable(
                name: "instances",
                newName: "Instances");

            migrationBuilder.RenameColumn(
                name: "root",
                table: "Instances",
                newName: "instance_path");

            migrationBuilder.RenameColumn(
                name: "enabled",
                table: "Instances",
                newName: "is_active");

            migrationBuilder.AlterColumn<string>(
                name: "name",
                table: "Instances",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "id",
                table: "Instances",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("Relational:ColumnOrder", 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Instances",
                table: "Instances",
                column: "id");
        }
    }
}
