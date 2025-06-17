using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class UpdatecheatingEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CtrCEvent",
                table: "CheatingEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MultiTabEvent",
                table: "CheatingEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PageSwitchEvent",
                table: "CheatingEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CtrCEvent",
                table: "CheatingEvents");

            migrationBuilder.DropColumn(
                name: "MultiTabEvent",
                table: "CheatingEvents");

            migrationBuilder.DropColumn(
                name: "PageSwitchEvent",
                table: "CheatingEvents");
        }
    }
}
