using Microsoft.EntityFrameworkCore;

namespace NetSimpleAuctioneer.API.Database
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
                // Avoid case sensitive issues in PostgreSQL
                entity.ToTable(nameof(Vehicle).ToLowerInvariant());
                entity.HasKey(v => v.Id);
                // Unique constraint
                entity.HasIndex(v => v.Id).IsUnique();

                entity.Property(v => v.Manufacturer).IsRequired();
                entity.Property(v => v.Model).IsRequired();
                entity.Property(v => v.Year).IsRequired();
                entity.Property(v => v.StartingBid).IsRequired();
                entity.Property(v => v.VehicleType).IsRequired();

                entity.Property(v => v.NumberOfDoors);
                entity.Property(v => v.NumberOfSeats);
                entity.Property(v => v.LoadCapacity);
            });

            // Auction Configuration
            modelBuilder.Entity<Auction>(entity =>
            {
                // Avoid case sensitive issues in PostgreSQL
                entity.ToTable(nameof(Auction).ToLowerInvariant());
                entity.HasKey(a => a.Id);
                // Unique constraint, only one vehicle per auction
                entity.HasIndex(a => a.VehicleId).IsUnique();

                entity.Property(a => a.StartDate).IsRequired();
                entity.Property(a => a.Status).IsRequired();

                entity.HasOne(a => a.Vehicle)
                      .WithMany()
                      .HasForeignKey(a => a.VehicleId);
            });

            // Bid Configuration
            modelBuilder.Entity<Bid>(entity =>
            {
                // Avoid case sensitive issues in PostgreSQL
                entity.ToTable(nameof(Bid).ToLowerInvariant());
                entity.HasKey(b => b.Id);

                entity.Property(b => b.BidAmount).IsRequired();
                entity.Property(b => b.BidderEmail).IsRequired();
                entity.Property(b => b.Timestamp).IsRequired();

                entity.HasOne(b => b.Auction)
                      .WithMany()
                      .HasForeignKey(b => b.AuctionId);
            });
        }
    }
}
