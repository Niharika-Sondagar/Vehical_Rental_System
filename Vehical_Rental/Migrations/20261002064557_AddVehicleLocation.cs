using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vehical_Rental.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Vehicles",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Vehicles",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Vehicles");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Vehicles");
        }
    }
}
