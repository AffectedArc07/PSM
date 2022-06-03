using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSM.Core.Migrations
{
    public partial class disable_to_archive : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.RenameColumn(name: "Disabled",
                                          table: "users",
                                          newName: "Archived");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(name: "Archived",
                                          table: "users",
                                          newName: "Disabled");
        }
    }
}
