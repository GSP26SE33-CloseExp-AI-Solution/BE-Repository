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
    public DbSet<CustomerFeedback> Feedbacks => Set<CustomerFeedback>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<DeliveryLog> DeliveryLogs => Set<DeliveryLog>();
    public DbSet<Supermarket> Supermarkets => Set<Supermarket>();
    public DbSet<SupermarketStaff> SupermarketStaffs => Set<SupermarketStaff>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductDetail> ProductDetails => Set<ProductDetail>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<StockLot> StockLots => Set<StockLot>();
    public DbSet<UnitOfMeasure> Units => Set<UnitOfMeasure>();
    public DbSet<InventoryDisposal> InventoryDisposals => Set<InventoryDisposal>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PricingHistory> AIPriceHistories => Set<PricingHistory>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<AIVerificationLog> AIVerificationLogs => Set<AIVerificationLog>();
    public DbSet<OrderPackaging> PackagingRecords => Set<OrderPackaging>();
    public DbSet<DeliveryTimeSlot> TimeSlots => Set<DeliveryTimeSlot>();
    public DbSet<CollectionPoint> PickupPoints => Set<CollectionPoint>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<DeliveryGroup> DeliveryGroups => Set<DeliveryGroup>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BarcodeProduct> BarcodeProducts => Set<BarcodeProduct>();
    public DbSet<MarketPrice> MarketPrices => Set<MarketPrice>();
    public DbSet<PriceFeedback> PriceFeedbacks => Set<PriceFeedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<AIVerificationLog>().HasKey(x => x.VerificationId);
        modelBuilder.Entity<DeliveryLog>().HasKey(x => x.DeliveryId);
        modelBuilder.Entity<UserImage>().HasKey(x => x.ImageId);
        modelBuilder.Entity<OrderPackaging>().HasKey(x => x.PackagingId);
        modelBuilder.Entity<SystemConfig>().HasKey(x => x.ConfigKey);
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.OtpCode).HasMaxLength(100);
            entity.Property(u => u.GoogleId).HasMaxLength(200);
            entity.HasIndex(u => u.GoogleId).IsUnique().HasFilter("\"GoogleId\" IS NOT NULL");
        });
        modelBuilder.Entity<StockLot>().HasKey(x => x.LotId);
        modelBuilder.Entity<PricingHistory>().HasKey(x => x.AIPriceId);
        modelBuilder.Entity<InventoryDisposal>().HasKey(x => x.DisposalId);
        modelBuilder.Entity<InventoryDisposal>().ToTable("InventoryDisposals");
        // modelBuilder.Entity<AIPriceHistory>().HasKey(x => x.AIPriceId);
        modelBuilder.Entity<UnitOfMeasure>().HasKey(x => x.UnitId);
        modelBuilder.Entity<CollectionPoint>().HasKey(x => x.PickupPointId);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Unit)
            .WithMany(u => u.Products)
            .HasForeignKey(p => p.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.ProductDetail)
            .WithOne(pd => pd.Product)
            .HasForeignKey<ProductDetail>(pd => pd.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.CategoryRef)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockLot>()
            .HasOne(pl => pl.Product)
            .WithMany(p => p.StockLots)
            .HasForeignKey(pl => pl.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockLot>()
            .HasOne(pl => pl.Unit)
            .WithMany()
            .HasForeignKey(pl => pl.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InventoryDisposal>()
            .HasOne(id => id.StockLot)
            .WithMany(pl => pl.InventoryDisposals)
            .HasForeignKey(id => id.LotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.ChildCategories)
            .HasForeignKey(c => c.ParentCatId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PricingHistory>()
            .HasOne(aph => aph.StockLot)
            .WithMany(pl => pl.AIPriceHistories)
            .HasForeignKey(aph => aph.LotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.StockLot)
            .WithMany(pl => pl.OrderItems)
            .HasForeignKey(oi => oi.LotId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DeliveryGroup>().HasKey(dg => dg.DeliveryGroupId);
        modelBuilder.Entity<DeliveryGroup>()
            .HasOne(dg => dg.DeliveryStaff)
            .WithMany()
            .HasForeignKey(dg => dg.DeliveryStaffId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<DeliveryGroup>()
            .HasOne(dg => dg.TimeSlot)
            .WithMany()
            .HasForeignKey(dg => dg.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>()
            .HasOne(o => o.DeliveryGroup)
            .WithMany(dg => dg.Orders)
            .HasForeignKey(o => o.DeliveryGroupId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Order>()
            .HasOne(o => o.CustomerAddress)
            .WithMany()
            .HasForeignKey(o => o.AddressId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CustomerAddress>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

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
