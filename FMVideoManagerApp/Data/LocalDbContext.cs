using FMVideoManagerApp.Models.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FMVideoManagerApp.Data
{
    public sealed class LocalDbContext : DbContext
    {
        public LocalDbContext(DbContextOptions<LocalDbContext> options)
            : base(options)
        {
        }

        public DbSet<LocalIndexedPath> LocalIndexedPaths => Set<LocalIndexedPath>();
        public DbSet<LocalFileLocation> LocalFileLocations => Set<LocalFileLocation>();
        public DbSet<PreviewCache> PreviewCaches => Set<PreviewCache>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureLocalIndexedPath(modelBuilder);
            ConfigureLocalFileLocation(modelBuilder);
            ConfigurePreviewCache(modelBuilder);
        }

        public override int SaveChanges()
        {
            ApplyLocalTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyLocalTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyLocalTimestamps()
        {
            DateTime now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<LocalIndexedPath>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAtUtc = now;
                }
            }

            foreach (var entry in ChangeTracker.Entries<LocalFileLocation>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.LastSeenUtc = now;
                }
            }

            foreach (var entry in ChangeTracker.Entries<PreviewCache>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAtUtc = now;
                    entry.Entity.LastAccessedAtUtc = now;
                }
            }
        }

        private static void ConfigureLocalIndexedPath(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LocalIndexedPath>(entity =>
            {
                entity.ToTable("local_indexed_path");

                entity.HasKey(x => x.Id)
                    .HasName("PK_local_indexed_path");

                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.ServerUserId)
                    .HasColumnName("server_user_id")
                    .IsRequired();

                entity.Property(x => x.Path)
                    .HasColumnName("path")
                    .IsRequired();

                entity.Property(x => x.IsEnabled)
                    .HasColumnName("is_enabled")
                    .HasDefaultValue(true);

                entity.Property(x => x.IncludeSubdirectories)
                    .HasColumnName("include_subdirectories")
                    .HasDefaultValue(true);

                entity.Property(x => x.CreatedAtUtc)
                    .HasColumnName("created_at_utc")
                    .IsRequired();

                entity.Property(x => x.LastScannedAtUtc)
                    .HasColumnName("last_scanned_at_utc");

                entity.HasIndex(x => new { x.ServerUserId, x.Path })
                    .IsUnique()
                    .HasDatabaseName("UQ_local_indexed_path_user_path");
            });
        }

        private static void ConfigureLocalFileLocation(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LocalFileLocation>(entity =>
            {
                entity.ToTable("local_file_location");

                entity.HasKey(x => x.Id)
                    .HasName("PK_local_file_location");

                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.ServerUserId)
                    .HasColumnName("server_user_id")
                    .IsRequired();

                entity.Property(x => x.ServerFileItemId)
                    .HasColumnName("server_file_item_id");

                entity.Property(x => x.ContentHash)
                    .HasColumnName("content_hash")
                    .HasMaxLength(64);

                entity.Property(x => x.LocalIndexedPathId)
                    .HasColumnName("local_indexed_path_id");

                entity.Property(x => x.Path)
                    .HasColumnName("path")
                    .IsRequired();

                entity.Property(x => x.Filename)
                    .HasColumnName("filename")
                    .IsRequired();

                entity.Property(x => x.SizeBytes)
                    .HasColumnName("size_bytes")
                    .IsRequired();

                entity.Property(x => x.LastModifiedUtc)
                    .HasColumnName("last_modified_utc")
                    .IsRequired();

                entity.Property(x => x.LastSeenUtc)
                    .HasColumnName("last_seen_utc")
                    .IsRequired();

                entity.Property(x => x.ExistsOnDisk)
                    .HasColumnName("exists_on_disk")
                    .HasDefaultValue(true);

                entity.HasOne(x => x.LocalIndexedPath)
                    .WithMany()
                    .HasForeignKey(x => x.LocalIndexedPathId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_local_file_location_indexed_path");

                entity.HasIndex(x => new { x.ServerUserId, x.Path })
                    .IsUnique()
                    .HasDatabaseName("UQ_local_file_location_user_path");

                entity.HasIndex(x => x.ContentHash)
                    .HasDatabaseName("IX_local_file_location_content_hash");

                entity.HasIndex(x => x.ServerFileItemId)
                    .HasDatabaseName("IX_local_file_location_server_file_item_id");
            });
        }

        private static void ConfigurePreviewCache(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PreviewCache>(entity =>
            {
                entity.ToTable("preview_cache");

                entity.HasKey(x => x.Id)
                    .HasName("PK_preview_cache");

                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.ServerUserId)
                    .HasColumnName("server_user_id")
                    .IsRequired();

                entity.Property(x => x.ServerFileItemId)
                    .HasColumnName("server_file_item_id");

                entity.Property(x => x.ContentHash)
                    .HasColumnName("content_hash")
                    .HasMaxLength(64);

                entity.Property(x => x.PreviewPath)
                    .HasColumnName("preview_path")
                    .IsRequired();

                entity.Property(x => x.Width)
                    .HasColumnName("width");

                entity.Property(x => x.Height)
                    .HasColumnName("height");

                entity.Property(x => x.CreatedAtUtc)
                    .HasColumnName("created_at_utc")
                    .IsRequired();

                entity.Property(x => x.LastAccessedAtUtc)
                    .HasColumnName("last_accessed_at_utc")
                    .IsRequired();

                entity.HasIndex(x => x.ServerFileItemId)
                    .HasDatabaseName("IX_preview_cache_server_file_item_id");

                entity.HasIndex(x => x.ContentHash)
                    .HasDatabaseName("IX_preview_cache_content_hash");

                entity.HasIndex(x => x.PreviewPath)
                    .IsUnique()
                    .HasDatabaseName("UQ_preview_cache_path");
            });
        }
    }
}