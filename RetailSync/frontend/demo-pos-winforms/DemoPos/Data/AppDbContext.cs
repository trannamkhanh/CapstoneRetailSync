using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DemoPos.Data.Entities;
using DemoPos.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DemoPos.Data;

public class AppDbContext : DbContext
{
	private readonly string _connectionString;

	public static string DefaultConnectionString => "Data Source=" + Path.Combine(AppContext.BaseDirectory, "pos.db");

	public DbSet<ProductEntity> Products => ((DbContext)this).Set<ProductEntity>();

	public DbSet<TerminalEntity> Terminals => ((DbContext)this).Set<TerminalEntity>();

	public DbSet<TransactionEntity> Transactions => ((DbContext)this).Set<TransactionEntity>();

	public DbSet<SoldItemEntity> SoldItems => ((DbContext)this).Set<SoldItemEntity>();

	public DbSet<ReturnedItemEntity> ReturnedItems => ((DbContext)this).Set<ReturnedItemEntity>();

	public DbSet<CancelledTransactionEntity> CancelledTransactions => ((DbContext)this).Set<CancelledTransactionEntity>();

	public DbSet<EventLogEntity> EventLogs => ((DbContext)this).Set<EventLogEntity>();

	public DbSet<AppSettingEntity> AppSettings => ((DbContext)this).Set<AppSettingEntity>();

	public AppDbContext(string? connectionString = null)
	{
		_connectionString = (string.IsNullOrWhiteSpace(connectionString) ? DefaultConnectionString : connectionString);
	}

	public AppDbContext(DbContextOptions<AppDbContext> options)
		: base((DbContextOptions)(object)options)
	{
		_connectionString = DefaultConnectionString;
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
		{
			SqliteDbContextOptionsBuilderExtensions.UseSqlite(optionsBuilder, _connectionString, (Action<SqliteDbContextOptionsBuilder>)null);
		}
	}

	public void Compact()
	{
		try
		{
			RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(((DbContext)this).Database, "PRAGMA wal_checkpoint(TRUNCATE);", Array.Empty<object>());
			RelationalDatabaseFacadeExtensions.ExecuteSqlRaw(((DbContext)this).Database, "PRAGMA journal_mode=DELETE;", Array.Empty<object>());
		}
		catch (SqliteException)
		{
		}
		RelationalDatabaseFacadeExtensions.CloseConnection(((DbContext)this).Database);
		SqliteConnection.ClearAllPools();
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);
		modelBuilder.Entity<ProductEntity>((Action<EntityTypeBuilder<ProductEntity>>)((EntityTypeBuilder<ProductEntity> entity) =>
		{
			entity.HasKey((Expression<Func<ProductEntity, object>>)((ProductEntity e) => e.Sku));
			RelationalPropertyBuilderExtensions.HasColumnType<decimal>(entity.Property<decimal>((Expression<Func<ProductEntity, decimal>>)((ProductEntity e) => e.UnitPrice)), "decimal(18,2)");
			entity.HasIndex((Expression<Func<ProductEntity, object>>)((ProductEntity e) => e.Barcode)).IsUnique(true);
		}));
		modelBuilder.Entity<TerminalEntity>((Action<EntityTypeBuilder<TerminalEntity>>)((EntityTypeBuilder<TerminalEntity> entity) =>
		{
			entity.HasKey((Expression<Func<TerminalEntity, object>>)((TerminalEntity e) => new { e.StoreId, e.PosId }));
		}));
		modelBuilder.Entity<TransactionEntity>((Action<EntityTypeBuilder<TransactionEntity>>)((EntityTypeBuilder<TransactionEntity> entity) =>
		{
			entity.HasKey((Expression<Func<TransactionEntity, object>>)((TransactionEntity e) => e.TransactionId));
			entity.HasIndex((Expression<Func<TransactionEntity, object>>)((TransactionEntity e) => new { e.StoreId, e.PosId, e.EventType }));
			entity.HasIndex((Expression<Func<TransactionEntity, object>>)((TransactionEntity e) => e.OriginalTransactionId));
		}));
		modelBuilder.Entity<SoldItemEntity>((Action<EntityTypeBuilder<SoldItemEntity>>)((EntityTypeBuilder<SoldItemEntity> entity) =>
		{
			entity.HasKey((Expression<Func<SoldItemEntity, object>>)((SoldItemEntity e) => e.Id));
			entity.HasIndex((Expression<Func<SoldItemEntity, object>>)((SoldItemEntity e) => e.TransactionId));
			entity.HasIndex((Expression<Func<SoldItemEntity, object>>)((SoldItemEntity e) => e.Sku));
		}));
		modelBuilder.Entity<ReturnedItemEntity>((Action<EntityTypeBuilder<ReturnedItemEntity>>)((EntityTypeBuilder<ReturnedItemEntity> entity) =>
		{
			entity.HasKey((Expression<Func<ReturnedItemEntity, object>>)((ReturnedItemEntity e) => e.Id));
			entity.HasIndex((Expression<Func<ReturnedItemEntity, object>>)((ReturnedItemEntity e) => e.OriginalTransactionId));
			entity.HasIndex((Expression<Func<ReturnedItemEntity, object>>)((ReturnedItemEntity e) => e.Sku));
		}));
		modelBuilder.Entity<CancelledTransactionEntity>((Action<EntityTypeBuilder<CancelledTransactionEntity>>)((EntityTypeBuilder<CancelledTransactionEntity> entity) =>
		{
			entity.HasKey((Expression<Func<CancelledTransactionEntity, object>>)((CancelledTransactionEntity e) => e.TransactionId));
		}));
		modelBuilder.Entity<EventLogEntity>((Action<EntityTypeBuilder<EventLogEntity>>)((EntityTypeBuilder<EventLogEntity> entity) =>
		{
			entity.HasKey((Expression<Func<EventLogEntity, object>>)((EventLogEntity e) => e.Id));
			entity.HasIndex((Expression<Func<EventLogEntity, object>>)((EventLogEntity e) => e.TransactionId));
			entity.HasIndex((Expression<Func<EventLogEntity, object>>)((EventLogEntity e) => e.CreatedAt));
		}));
		modelBuilder.Entity<AppSettingEntity>((Action<EntityTypeBuilder<AppSettingEntity>>)((EntityTypeBuilder<AppSettingEntity> entity) =>
		{
			entity.HasKey((Expression<Func<AppSettingEntity, object>>)((AppSettingEntity e) => e.Key));
		}));
	}

	public async Task EnsureCreatedAndSeedAsync(AppConfig config, CancellationToken cancellationToken = default(CancellationToken))
	{
		await ((DbContext)this).Database.EnsureCreatedAsync(cancellationToken);
		if (!(await EntityFrameworkQueryableExtensions.AnyAsync<ProductEntity>((IQueryable<ProductEntity>)Products, cancellationToken)))
		{
			IEnumerable<ProductEntity> products = from p in ProductCatalog.CreateProducts()
				select new ProductEntity
				{
					Sku = p.Sku,
					Barcode = p.Barcode,
					Name = p.Name,
					UnitPrice = p.UnitPrice,
					InitialStock = p.InitialStock,
					CurrentStock = p.InitialStock
				};
			await Products.AddRangeAsync(products, cancellationToken);
		}
		if (!(await EntityFrameworkQueryableExtensions.AnyAsync<TerminalEntity>((IQueryable<TerminalEntity>)Terminals, cancellationToken)))
		{
			IEnumerable<TerminalEntity> terminals = config.Terminals.Select((TerminalConfig t) => new TerminalEntity
			{
				StoreId = t.StoreId,
				PosId = t.PosId,
				StoreName = t.StoreName,
				Token = t.Token,
				IsActive = true
			});
			await Terminals.AddRangeAsync(terminals, cancellationToken);
		}
		await ((DbContext)this).SaveChangesAsync(cancellationToken);
	}
}
