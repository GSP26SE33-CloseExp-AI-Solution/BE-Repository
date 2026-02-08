using CloseExpAISolution.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CloseExpAISolution.Infrastructure.Context;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserImage> UserImages => Set<UserImage>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DeliveryRecord> DeliveryRecords => Set<DeliveryRecord>();
    public DbSet<Supermarket> Supermarkets => Set<Supermarket>();
    public DbSet<MarketStaff> MarketStaff => Set<MarketStaff>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Pricing> Pricings => Set<Pricing>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductLot> ProductLots => Set<ProductLot>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<OverdueRecord> OverdueRecords => Set<OverdueRecord>();
    public DbSet<AIPriceHistory> AIPriceHistories => Set<AIPriceHistory>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<AIVerificationLog> AIVerificationLogs => Set<AIVerificationLog>();
    public DbSet<PackagingRecord> PackagingRecords => Set<PackagingRecord>();
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<PickupPoint> PickupPoints => Set<PickupPoint>();
    public DbSet<DoorPickup> DoorPickups => Set<DoorPickup>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BarcodeProduct> BarcodeProducts => Set<BarcodeProduct>();
    public DbSet<MarketPrice> MarketPrices => Set<MarketPrice>();
    public DbSet<PriceFeedback> PriceFeedbacks => Set<PriceFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<AIVerificationLog>().HasKey(x => x.VerificationId);
        modelBuilder.Entity<DeliveryRecord>().HasKey(x => x.DeliveryId);
        modelBuilder.Entity<UserImage>().HasKey(x => x.ImageId);
        modelBuilder.Entity<PackagingRecord>().HasKey(x => x.PackagingId);
        modelBuilder.Entity<SystemConfig>().HasKey(x => x.ConfigKey);
        modelBuilder.Entity<ProductLot>().HasKey(x => x.LotId);
        modelBuilder.Entity<OverdueRecord>().HasKey(x => x.OverdueId);
        modelBuilder.Entity<AIPriceHistory>().HasKey(x => x.AIPriceId);
        modelBuilder.Entity<Unit>().HasKey(x => x.UnitId);
        modelBuilder.Entity<Pricing>().HasKey(x => x.PricingId);

        modelBuilder.Entity<Pricing>()
            .HasOne(pr => pr.Product)
            .WithOne(p => p.Pricing)
            .HasForeignKey<Pricing>(pr => pr.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductLot>()
            .HasOne(pl => pl.Product)
            .WithMany(p => p.ProductLots)
            .HasForeignKey(pl => pl.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductLot>()
            .HasOne(pl => pl.Unit)
            .WithMany(u => u.ProductLots)
            .HasForeignKey(pl => pl.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OverdueRecord>()
            .HasOne(or => or.ProductLot)
            .WithMany(pl => pl.OverdueRecords)
            .HasForeignKey(or => or.LotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AIPriceHistory>()
            .HasOne(aph => aph.ProductLot)
            .WithMany(pl => pl.AIPriceHistories)
            .HasForeignKey(aph => aph.LotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.ProductLot)
            .WithMany(pl => pl.OrderItems)
            .HasForeignKey(oi => oi.LotId)
            .OnDelete(DeleteBehavior.Restrict);

        // BarcodeProduct configuration
        modelBuilder.Entity<BarcodeProduct>().HasKey(bp => bp.BarcodeProductId);
        modelBuilder.Entity<BarcodeProduct>().HasIndex(bp => bp.Barcode).IsUnique();
        modelBuilder.Entity<BarcodeProduct>().Property(bp => bp.Barcode).HasMaxLength(20);
        modelBuilder.Entity<BarcodeProduct>().Property(bp => bp.ProductName).HasMaxLength(500);
        modelBuilder.Entity<BarcodeProduct>().Property(bp => bp.Brand).HasMaxLength(200);
        modelBuilder.Entity<BarcodeProduct>().Property(bp => bp.Category).HasMaxLength(200);
        modelBuilder.Entity<BarcodeProduct>().Property(bp => bp.Country).HasMaxLength(100);
        modelBuilder.Entity<BarcodeProduct>().Property(bp => bp.Gs1Prefix).HasMaxLength(10);
        modelBuilder.Entity<BarcodeProduct>().Property(bp => bp.Source).HasMaxLength(50);
        modelBuilder.Entity<BarcodeProduct>().Property(bp => bp.Status).HasMaxLength(20);

        // MarketPrice configuration
        modelBuilder.Entity<MarketPrice>().HasKey(mp => mp.MarketPriceId);
        modelBuilder.Entity<MarketPrice>().HasIndex(mp => mp.Barcode);
        modelBuilder.Entity<MarketPrice>().HasIndex(mp => new { mp.Barcode, mp.Source, mp.StoreName }).IsUnique();
        modelBuilder.Entity<MarketPrice>().Property(mp => mp.Barcode).HasMaxLength(20);
        modelBuilder.Entity<MarketPrice>().Property(mp => mp.ProductName).HasMaxLength(500);
        modelBuilder.Entity<MarketPrice>().Property(mp => mp.Source).HasMaxLength(50);
        modelBuilder.Entity<MarketPrice>().Property(mp => mp.StoreName).HasMaxLength(200);
        modelBuilder.Entity<MarketPrice>().Property(mp => mp.Region).HasMaxLength(100);
        modelBuilder.Entity<MarketPrice>().Property(mp => mp.Status).HasMaxLength(20);
        modelBuilder.Entity<MarketPrice>().Property(mp => mp.Price).HasPrecision(18, 2);
        modelBuilder.Entity<MarketPrice>().Property(mp => mp.OriginalPrice).HasPrecision(18, 2);

        // PriceFeedback configuration
        modelBuilder.Entity<PriceFeedback>().HasKey(pf => pf.Id);
        modelBuilder.Entity<PriceFeedback>().HasIndex(pf => pf.Barcode);
        modelBuilder.Entity<PriceFeedback>().HasIndex(pf => pf.CreatedAt);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.Barcode).HasMaxLength(20);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.ProductName).HasMaxLength(500);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.Category).HasMaxLength(100);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.RejectionReason).HasMaxLength(500);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.StaffFeedback).HasMaxLength(1000);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.StaffId).HasMaxLength(100);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.MarketPriceSource).HasMaxLength(50);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.SuggestedPrice).HasPrecision(18, 2);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.FinalPrice).HasPrecision(18, 2);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.OriginalPrice).HasPrecision(18, 2);
        modelBuilder.Entity<PriceFeedback>().Property(pf => pf.MarketPriceRef).HasPrecision(18, 2);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                var createdAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProperty != null && createdAtProperty.CurrentValue == null)
                {
                    createdAtProperty.CurrentValue = DateTime.UtcNow;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                var updatedAtProperty = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProperty != null)
                {
                    updatedAtProperty.CurrentValue = DateTime.UtcNow;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
