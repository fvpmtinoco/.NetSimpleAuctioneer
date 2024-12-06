using Microsoft.EntityFrameworkCore;
using NetSimpleAuctioneer.API.Domain;

namespace NetSimpleAuctioneer.API.Infrastructure.Data
{
    public class AuctioneerDbContext(DbContextOptions<AuctioneerDbContext> options) : DbContext(options)
    {
        public DbSet<Vehicle> Vehicles { get; set; } = default!;
        public DbSet<Auction> Auctions { get; set; } = default!;
        public DbSet<Bid> Bids { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Vehicle Configuration
            modelBuilder.Entity<Vehicle>(entity =>
            {
                // Table name to lowercase
                entity.ToTable(nameof(Vehicle).ToLowerInvariant());
                entity.HasKey(v => v.Id);
                // Unique constraint
                entity.HasIndex(v => v.Id).IsUnique();

                // Column names to lowercase
                entity.Property(b => b.Id).HasColumnName("id");
                entity.Property(v => v.Manufacturer).IsRequired().HasColumnName("manufacturer");
                entity.Property(v => v.Model).IsRequired().HasColumnName("model");
                entity.Property(v => v.Year).IsRequired().HasColumnName("year");
                entity.Property(v => v.StartingBid).IsRequired().HasColumnName("startingbid");
                entity.Property(v => v.VehicleType).IsRequired().HasColumnName("vehicletype");

                entity.Property(v => v.NumberOfDoors).HasColumnName("numberofdoors");
                entity.Property(v => v.NumberOfSeats).HasColumnName("numberofseats");
                entity.Property(v => v.LoadCapacity).HasColumnName("loadcapacity");
            });

            // Auction Configuration
            modelBuilder.Entity<Auction>(entity =>
            {
                // Table name to lowercase
                entity.ToTable(nameof(Auction).ToLowerInvariant());
                entity.HasKey(a => a.Id);

                entity.Property(b => b.Id).HasColumnName("id");
                entity.Property(a => a.StartDate).IsRequired().HasColumnName("startdate");
                entity.Property(a => a.EndDate).HasColumnName("enddate");
                entity.Property(a => a.VehicleId).IsRequired().HasColumnName("vehicleid");

                entity.HasOne(a => a.Vehicle)
                    .WithMany(v => v.Auctions)
                    .HasForeignKey(a => a.VehicleId);
            });

            // Bid Configuration
            modelBuilder.Entity<Bid>(entity =>
            {
                entity.ToTable(nameof(Bid).ToLowerInvariant());
                entity.HasKey(b => b.Id);

                entity.Property(b => b.Id).HasColumnName("id");
                entity.Property(b => b.BidAmount).IsRequired().HasColumnName("bidamount");
                entity.Property(b => b.BidderEmail).IsRequired().HasColumnName("biddersemail");
                entity.Property(b => b.Timestamp).IsRequired().HasColumnName("timestamp");

                entity.Property(b => b.AuctionId).HasColumnName("auctionid");
            });

            // Define a unique constraint on the combination of VehicleId and EndDate
            modelBuilder.Entity<Auction>()
                .HasIndex(a => new { a.VehicleId, a.EndDate })
                .IsUnique()
                .HasDatabaseName("UniqueActiveAuction");
        }
    }
}
