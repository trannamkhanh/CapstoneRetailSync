using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using DemoPos.Data.Entities;
using DemoPos.Models;

namespace DemoPos.Data;

/// <summary>
/// EF Core DbContext trên SQLite (file pos.db).
/// Lưu toàn bộ trạng thái POS: sản phẩm, terminal, giao dịch, dòng bán/trả/hủy và audit log.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>Đường dẫn mặc định của file database (cạnh file exe).</summary>
    public static string DefaultConnectionString =>
        $"Data Source={Path.Combine(AppContext.BaseDirectory, "pos.db")}";

    private readonly string _connectionString;

    public AppDbContext(string? connectionString = null)
    {
        _connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? DefaultConnectionString
            : connectionString;
    }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        _connectionString = DefaultConnectionString;
    }

    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    public DbSet<TerminalEntity> Terminals => Set<TerminalEntity>();

    public DbSet<TransactionEntity> Transactions => Set<TransactionEntity>();

    public DbSet<SoldItemEntity> SoldItems => Set<SoldItemEntity>();

    public DbSet<ReturnedItemEntity> ReturnedItems => Set<ReturnedItemEntity>();

    public DbSet<CancelledTransactionEntity> CancelledTransactions => Set<CancelledTransactionEntity>();

    public DbSet<EventLogEntity> EventLogs => Set<EventLogEntity>();

    public DbSet<AppSettingEntity> AppSettings => Set<AppSettingEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // journal_mode=DELETE (thay vì WAL): toàn bộ dữ liệu nằm trong 1 file pos.db duy nhất,
            // không sinh thêm pos.db-wal / pos.db-shm -> copy đi đóng gói an toàn.
            optionsBuilder.UseSqlite(_connectionString);
        }
    }

    /// <summary>
    /// Đóng kết nối và trả file về trạng thái tự chứa (checkpoint WAL + journal_mode=DELETE).
    /// Gọi trước khi copy file .db đi đóng gói.
    /// </summary>
    public void Compact()
    {
        try
        {
            Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);");
            Database.ExecuteSqlRaw("PRAGMA journal_mode=DELETE;");
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            // Database chưa mở / không phải WAL: bỏ qua.
        }

        Database.CloseConnection();
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.HasKey(e => e.Sku);
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.HasIndex(e => e.Barcode).IsUnique();
        });

        modelBuilder.Entity<TerminalEntity>(entity =>
        {
            entity.HasKey(e => new { e.StoreId, e.PosId });
        });

        modelBuilder.Entity<TransactionEntity>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
            entity.HasIndex(e => new { e.StoreId, e.PosId, e.EventType });
            entity.HasIndex(e => e.OriginalTransactionId);
        });

        modelBuilder.Entity<SoldItemEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.Sku);
        });

        modelBuilder.Entity<ReturnedItemEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OriginalTransactionId);
            entity.HasIndex(e => e.Sku);
        });

        modelBuilder.Entity<CancelledTransactionEntity>(entity =>
        {
            entity.HasKey(e => e.TransactionId);
        });

        modelBuilder.Entity<EventLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<AppSettingEntity>(entity =>
        {
            entity.HasKey(e => e.Key);
        });
    }

    /// <summary>
    /// Tạo database (nếu chưa có) và seed danh mục sản phẩm + terminal.
    /// Chỉ seed khi bảng tương ứng còn rỗng nên gọi nhiều lần vẫn an toàn.
    /// </summary>
    public async Task EnsureCreatedAndSeedAsync(AppConfig config, CancellationToken cancellationToken = default)
    {
        await Database.EnsureCreatedAsync(cancellationToken);

        if (!await Products.AnyAsync(cancellationToken))
        {
            var products = ProductCatalog.CreateProducts().Select(p => new ProductEntity
            {
                Sku = p.Sku,
                Barcode = p.Barcode,
                Name = p.Name,
                UnitPrice = p.UnitPrice,
                InitialStock = p.InitialStock,
                CurrentStock = p.InitialStock
            });

            await Products.AddRangeAsync(products, cancellationToken);
        }

        if (!await Terminals.AnyAsync(cancellationToken))
        {
            var terminals = config.Terminals.Select(t => new TerminalEntity
            {
                StoreId = t.StoreId,
                PosId = t.PosId,
                StoreName = t.StoreName,
                Token = t.Token,
                IsActive = true
            });

            await Terminals.AddRangeAsync(terminals, cancellationToken);
        }

        await SaveChangesAsync(cancellationToken);
    }
}
