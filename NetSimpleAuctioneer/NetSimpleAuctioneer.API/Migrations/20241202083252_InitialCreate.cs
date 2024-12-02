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
                name: "vehicle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Manufacturer = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    StartingBid = table.Column<decimal>(type: "numeric", nullable: false),
                    VehicleType = table.Column<int>(type: "integer", nullable: false),
                    NumberOfDoors = table.Column<int>(type: "integer", nullable: true),
                    NumberOfSeats = table.Column<int>(type: "integer", nullable: true),
                    LoadCapacity = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "auction",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_auction_vehicle_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "vehicle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bid",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    BidAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    BidderEmail = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bid", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bid_auction_AuctionId",
                        column: x => x.AuctionId,
                        principalTable: "auction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_auction_VehicleId",
                table: "auction",
                column: "VehicleId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_bid_AuctionId",
                table: "bid",
                column: "AuctionId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_Id",
                table: "vehicle",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bid");

            migrationBuilder.DropTable(
                name: "auction");

            migrationBuilder.DropTable(
                name: "vehicle");
        }
    }
}
