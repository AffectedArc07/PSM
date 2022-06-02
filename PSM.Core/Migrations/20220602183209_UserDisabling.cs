using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PSM.Core.Migrations
{
    public partial class UserDisabling : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder) {
            migrationBuilder.AddColumn<bool>("Disabled", "users", "tinyint(1)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
