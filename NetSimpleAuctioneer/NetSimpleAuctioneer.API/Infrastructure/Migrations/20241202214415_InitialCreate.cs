using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NetSimpleAuctioneer.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auction",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicleid = table.Column<Guid>(type: "uuid", nullable: false),
                    startdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    enddate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "bid",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    auctionid = table.Column<Guid>(type: "uuid", nullable: false),
                    bidamount = table.Column<decimal>(type: "numeric", nullable: false),
                    biddersemail = table.Column<string>(type: "text", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bid", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer = table.Column<string>(type: "text", nullable: false),
                    model = table.Column<string>(type: "text", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    startingbid = table.Column<decimal>(type: "numeric", nullable: false),
                    vehicletype = table.Column<int>(type: "integer", nullable: false),
                    numberofdoors = table.Column<int>(type: "integer", nullable: true),
                    numberofseats = table.Column<int>(type: "integer", nullable: true),
                    loadcapacity = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "UniqueActiveAuction",
                table: "auction",
                columns: new[] { "vehicleid", "enddate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_id",
                table: "vehicle",
                column: "id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auction");

            migrationBuilder.DropTable(
                name: "bid");

            migrationBuilder.DropTable(
                name: "vehicle");
        }
    }
}
