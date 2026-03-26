using CloseExpAISolution.Domain.Entities;
using CloseExpAISolution.Domain.Enums;
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
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<InventoryDisposal> InventoryDisposals => Set<InventoryDisposal>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<PricingHistory> PricingHistories => Set<PricingHistory>();
    public DbSet<SystemConfig> SystemConfigs => Set<SystemConfig>();
    public DbSet<AIVerificationLog> AIVerificationLogs => Set<AIVerificationLog>();
    public DbSet<OrderPackaging> PackagingRecords => Set<OrderPackaging>();
    public DbSet<DeliveryTimeSlot> DeliveryTimeSlots => Set<DeliveryTimeSlot>();
    public DbSet<CollectionPoint> CollectionPoints => Set<CollectionPoint>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<Promotion> Promotions => Set<Promotion>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<DeliveryGroup> DeliveryGroups => Set<DeliveryGroup>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<MarketPrice> MarketPrices => Set<MarketPrice>();
    public DbSet<OrderStatusLog> OrderStatusLogs => Set<OrderStatusLog>();
    public DbSet<PromotionUsage> PromotionUsages => Set<PromotionUsage>();
    public DbSet<DeliveryFeeConfig> DeliveryFeeConfigs => Set<DeliveryFeeConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<AIVerificationLog>().HasKey(x => x.VerificationId);
        modelBuilder.Entity<DeliveryLog>().HasKey(x => x.DeliveryId);
        modelBuilder.Entity<DeliveryLog>().Property(x => x.DeliveryLatitude).HasPrecision(10, 7);
        modelBuilder.Entity<DeliveryLog>().Property(x => x.DeliveryLongitude).HasPrecision(10, 7);
        modelBuilder.Entity<UserImage>().HasKey(x => x.ImageId);
        modelBuilder.Entity<OrderPackaging>().HasKey(x => x.PackagingId);
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(x => x.TransactionId);
            entity.HasIndex(x => x.PayOSOrderCode)
                .IsUnique()
                .HasFilter("\"PayOSOrderCode\" IS NOT NULL");
        });
        modelBuilder.Entity<SystemConfig>().HasKey(x => x.ConfigKey);
        modelBuilder.Entity<StockLot>().HasKey(x => x.LotId);
        modelBuilder.Entity<PricingHistory>().HasKey(x => x.AIPriceId);
        modelBuilder.Entity<InventoryDisposal>().HasKey(x => x.DisposalId);
        modelBuilder.Entity<UnitOfMeasure>().HasKey(x => x.UnitId);
        modelBuilder.Entity<CollectionPoint>().HasKey(x => x.CollectionId);
        modelBuilder.Entity<DeliveryTimeSlot>().HasKey(x => x.TimeSlotId);
        modelBuilder.Entity<DeliveryGroup>().HasKey(dg => dg.DeliveryGroupId);
        modelBuilder.Entity<MarketPrice>().HasKey(mp => mp.MarketPriceId);
        modelBuilder.Entity<Refund>().HasKey(r => r.RefundId);
        modelBuilder.Entity<OrderStatusLog>().HasKey(o => o.LogId);
        modelBuilder.Entity<PromotionUsage>().HasKey(pu => pu.UsageId);
        modelBuilder.Entity<DeliveryFeeConfig>().HasKey(d => d.ConfigId);

        modelBuilder.Entity<InventoryDisposal>().ToTable("InventoryDisposals");
        modelBuilder.Entity<CollectionPoint>().ToTable("CollectionPoints");
        modelBuilder.Entity<DeliveryTimeSlot>().ToTable("DeliveryTimeSlots");
        modelBuilder.Entity<OrderPackaging>().ToTable("OrderPackaging");

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(u => u.OtpCode).HasMaxLength(100);
            entity.Property(u => u.GoogleId).HasMaxLength(200);
            entity.HasIndex(u => u.GoogleId).IsUnique().HasFilter("\"GoogleId\" IS NOT NULL");
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Phone);
            entity.HasIndex(u => u.Status);
        });

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

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.Barcode).IsUnique();
            entity.HasIndex(p => new { p.SupermarketId, p.Status });
            entity.HasIndex(p => p.CategoryId);
        });

        modelBuilder.Entity<Promotion>()
            .HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.Property(p => p.Code).HasMaxLength(50);
            entity.Property(p => p.DiscountType).HasMaxLength(50);
            entity.Property(p => p.DiscountValue).HasPrecision(18, 2);
            entity.Property(p => p.MinOrderAmount).HasPrecision(18, 2);
            entity.Property(p => p.MaxDiscountAmount).HasPrecision(18, 2);
            entity.HasIndex(p => p.Code).IsUnique();
            entity.HasIndex(p => new { p.Status, p.StartDate, p.EndDate });
        });

        modelBuilder.Entity<StockLot>()
            .HasOne(pl => pl.Product)
            .WithMany(p => p.StockLots)
            .HasForeignKey(pl => pl.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<StockLot>()
            .HasOne(pl => pl.Unit)
            .WithMany(u => u.StockLots)
            .HasForeignKey(pl => pl.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StockLot>(entity =>
        {
            entity.HasIndex(l => new { l.ProductId, l.Status });
            entity.HasIndex(l => l.ExpiryDate);
        });

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
            .WithMany(pl => pl.PricingHistories)
            .HasForeignKey(aph => aph.LotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PricingHistory>()
            .HasOne(ph => ph.SupermarketRef)
            .WithMany()
            .HasForeignKey(ph => ph.SupermarketId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PricingHistory>(entity =>
        {
            entity.HasIndex(ph => new { ph.LotId, ph.CreatedAt });
            entity.Property(ph => ph.AIConfidence).HasPrecision(5, 4);
            entity.Property(ph => ph.SuggestedPrice).HasPrecision(18, 2);
            entity.Property(ph => ph.MarketMinPrice).HasPrecision(18, 2);
            entity.Property(ph => ph.MarketMaxPrice).HasPrecision(18, 2);
            entity.Property(ph => ph.MarketAvgPrice).HasPrecision(18, 2);
            entity.Property(ph => ph.MarketPriceRef).HasPrecision(18, 2);
            entity.Property(ph => ph.RejectionReason).HasMaxLength(500);
            entity.Property(ph => ph.Feedback).HasMaxLength(1000);
            entity.Property(ph => ph.Source).HasMaxLength(50);
        });

        modelBuilder.Entity<AIVerificationLog>()
            .HasOne(a => a.Product)
            .WithMany(p => p.AIVerificationLogs)
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.StockLot)
            .WithMany(pl => pl.OrderItems)
            .HasForeignKey(oi => oi.LotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasIndex(oi => oi.OrderId);
            entity.HasIndex(oi => oi.LotId);
        });

        modelBuilder.Entity<DeliveryGroup>().Property(x => x.CenterLatitude).HasPrecision(10, 7);
        modelBuilder.Entity<DeliveryGroup>().Property(x => x.CenterLongitude).HasPrecision(10, 7);
        modelBuilder.Entity<DeliveryGroup>()
            .HasOne(dg => dg.DeliveryStaff)
            .WithMany()
            .HasForeignKey(dg => dg.DeliveryStaffId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<DeliveryGroup>()
            .HasOne(dg => dg.DeliveryTimeSlot)
            .WithMany()
            .HasForeignKey(dg => dg.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.DeliveryTimeSlot)
            .WithMany(ts => ts.Orders)
            .HasForeignKey(o => o.TimeSlotId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>()
            .HasOne(o => o.CollectionPoint)
            .WithMany(cp => cp.Orders)
            .HasForeignKey(o => o.CollectionId)
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
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Promotion)
            .WithMany(p => p.Orders)
            .HasForeignKey(o => o.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => new { o.UserId, o.Status });
            entity.HasIndex(o => o.OrderCode).IsUnique();
            entity.HasIndex(o => o.OrderDate);
            entity.HasIndex(o => o.DeliveryGroupId);
        });

        modelBuilder.Entity<CustomerAddress>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CustomerAddress>().Property(x => x.Latitude).HasPrecision(10, 7);
        modelBuilder.Entity<CustomerAddress>().Property(x => x.Longitude).HasPrecision(10, 7);

        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.HasIndex(ca => ca.UserId);
            entity.Property(ca => ca.Latitude).HasPrecision(10, 7);
            entity.Property(ca => ca.Longitude).HasPrecision(10, 7);
        });

        modelBuilder.Entity<CollectionPoint>(entity =>
        {
            entity.Property(cp => cp.Latitude).HasPrecision(10, 7);
            entity.Property(cp => cp.Longitude).HasPrecision(10, 7);
        });

        modelBuilder.Entity<Supermarket>(entity =>
        {
            entity.HasIndex(s => s.Status);
            entity.Property(s => s.Latitude).HasPrecision(10, 7);
            entity.Property(s => s.Longitude).HasPrecision(10, 7);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasIndex(t => t.OrderId);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasIndex(n => new { n.UserId, n.IsRead });
        });

        modelBuilder.Entity<DeliveryLog>(entity =>
        {
            entity.HasIndex(dl => dl.OrderId);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasIndex(pi => pi.ProductId);
        });

        modelBuilder.Entity<MarketPrice>(entity =>
        {
            entity.HasIndex(mp => mp.Barcode);
            entity.HasIndex(mp => new { mp.Barcode, mp.Source, mp.StoreName }).IsUnique();
            entity.Property(mp => mp.Barcode).HasMaxLength(20);
            entity.Property(mp => mp.ProductName).HasMaxLength(500);
            entity.Property(mp => mp.Source).HasMaxLength(50);
            entity.Property(mp => mp.StoreName).HasMaxLength(200);
            entity.Property(mp => mp.Region).HasMaxLength(100);
            entity.Property(mp => mp.Price).HasPrecision(18, 2);
            entity.Property(mp => mp.OriginalPrice).HasPrecision(18, 2);
            entity.Property(mp => mp.Confidence).HasPrecision(5, 4);
        });

        modelBuilder.Entity<Refund>()
            .HasOne(r => r.Order)
            .WithMany(o => o.Refunds)
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Refund>()
            .HasOne(r => r.Transaction)
            .WithMany(t => t.Refunds)
            .HasForeignKey(r => r.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OrderStatusLog>()
            .HasOne(os => os.Order)
            .WithMany(o => o.StatusLogs)
            .HasForeignKey(os => os.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<OrderStatusLog>(entity =>
        {
            entity.HasIndex(os => os.OrderId);
        });

        modelBuilder.Entity<PromotionUsage>()
            .HasOne(pu => pu.Promotion)
            .WithMany(p => p.PromotionUsages)
            .HasForeignKey(pu => pu.PromotionId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PromotionUsage>()
            .HasOne(pu => pu.User)
            .WithMany(u => u.PromotionUsages)
            .HasForeignKey(pu => pu.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PromotionUsage>(entity =>
        {
            entity.HasIndex(pu => new { pu.PromotionId, pu.UserId });
            entity.HasIndex(pu => new { pu.PromotionId, pu.UserId, pu.OrderId }).IsUnique();
            entity.HasIndex(pu => new { pu.PromotionId, pu.UsedAt });
            entity.HasIndex(pu => new { pu.UserId, pu.UsedAt });
            entity.Property(pu => pu.DiscountAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<DeliveryFeeConfig>(entity =>
        {
            entity.Property(d => d.MinDistance).HasPrecision(10, 2);
            entity.Property(d => d.MaxDistance).HasPrecision(10, 2);
            entity.Property(d => d.BaseFee).HasPrecision(18, 2);
            entity.Property(d => d.FeePerKm).HasPrecision(18, 2);
        });

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
