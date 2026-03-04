using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Luxury.Migrations
{
    /// <inheritdoc />
    public partial class AddTrangThaiToSudungDichvu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TrangThai",
                table: "SUDUNG_DICHVU",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TrangThai",
                table: "SUDUNG_DICHVU");
        }
    }
}
