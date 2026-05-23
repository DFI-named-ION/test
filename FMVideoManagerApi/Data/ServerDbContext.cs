using FMVideoManagerApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace FMVideoManagerApi.Data
{
    public sealed class ServerDbContext : DbContext
    {
        public ServerDbContext(DbContextOptions<ServerDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<HierarchyNode> HierarchyNodes => Set<HierarchyNode>();
        public DbSet<GroupItem> GroupItems => Set<GroupItem>();
        public DbSet<FileItem> FileItems => Set<FileItem>();
        public DbSet<FileContent> FileContents => Set<FileContent>();
        public DbSet<Tag> Tags => Set<Tag>();
        public DbSet<TagAlias> TagAliases => Set<TagAlias>();
        public DbSet<NodeTag> NodeTags => Set<NodeTag>();
        public DbSet<NodeAlias> NodeAliases => Set<NodeAlias>();
        public DbSet<CloudProviderAccount> CloudProviderAccounts => Set<CloudProviderAccount>();
        public DbSet<StorageReference> StorageReferences => Set<StorageReference>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            ConfigureUser(modelBuilder);
            ConfigureHierarchyNode(modelBuilder);
            ConfigureGroupItem(modelBuilder);
            ConfigureFileItem(modelBuilder);
            ConfigureFileContent(modelBuilder);
            ConfigureNodeAlias(modelBuilder);
            ConfigureTag(modelBuilder);
            ConfigureTagAlias(modelBuilder);
            ConfigureNodeTag(modelBuilder);
            ConfigureCloudProviderAccount(modelBuilder);
            ConfigureStorageReference(modelBuilder);
        }

        private static void ConfigureUser(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.ToTable("user");

                entity.HasKey(x => x.Id)
                    .HasName("PK_user");

                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.Login)
                    .HasColumnName("login")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.Alias)
                    .HasColumnName("alias")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.PasswordHash)
                    .HasColumnName("password_hash")
                    .HasMaxLength(512)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .HasColumnName("created_at_utc")
                    .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                    .HasColumnName("updated_at_utc")
                    .IsRequired();

                entity.HasIndex(x => x.Login)
                    .IsUnique()
                    .HasDatabaseName("UQ_user_login");
            });
        }

        private static void ConfigureHierarchyNode(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<HierarchyNode>(entity =>
            {
                entity.ToTable("hierarchy_node", table =>
                {
                    table.HasCheckConstraint(
                        "CK_hierarchy_node_not_self_parent",
                        "parent_node_id IS NULL OR parent_node_id <> id");

                    table.HasCheckConstraint(
                        "CK_hierarchy_node_type",
                        "node_type IN ('Group', 'File')");
                });

                entity.HasKey(x => x.Id)
                    .HasName("PK_hierarchy_node");

                entity.HasAlternateKey(x => new { x.Id, x.UserId })
                    .HasName("UQ_hierarchy_node_id_user");

                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

                entity.Property(x => x.ParentNodeId)
                    .HasColumnName("parent_node_id");

                entity.Property(x => x.NodeType)
                    .HasColumnName("node_type")
                    .HasConversion<string>()
                    .HasMaxLength(16)
                    .IsRequired();

                entity.Property(x => x.Title)
                    .HasColumnName("title")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.SortOrder)
                    .HasColumnName("sort_order")
                    .HasDefaultValue(0);

                entity.Property(x => x.CreatedAtUtc)
                    .HasColumnName("created_at_utc")
                    .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                    .HasColumnName("updated_at_utc")
                    .IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.HierarchyNodes)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_hierarchy_node_user");

                entity.HasOne(x => x.ParentNode)
                    .WithMany(x => x.ChildNodes)
                    .HasForeignKey(x => new { x.ParentNodeId, x.UserId })
                    .HasPrincipalKey(x => new { x.Id, x.UserId })
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_hierarchy_node_parent_same_user");

                entity.HasIndex(x => x.UserId)
                    .HasDatabaseName("IX_hierarchy_node_user_id");

                entity.HasIndex(x => x.ParentNodeId)
                    .HasDatabaseName("IX_hierarchy_node_parent_node_id");

                entity.HasIndex(x => new { x.UserId, x.ParentNodeId })
                    .HasDatabaseName("IX_hierarchy_node_user_parent");

                entity.HasIndex(x => new { x.UserId, x.NodeType })
                    .HasDatabaseName("IX_hierarchy_node_user_type");

                entity.HasIndex(x => x.Title)
                    .HasDatabaseName("IX_hierarchy_node_title");
            });
        }

        private static void ConfigureGroupItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GroupItem>(entity =>
            {
                entity.ToTable("group_item");

                entity.HasKey(x => x.NodeId)
                    .HasName("PK_group_item");

                entity.Property(x => x.NodeId)
                    .HasColumnName("node_id");

                entity.Property(x => x.Description)
                    .HasColumnName("description")
                    .HasMaxLength(1000);

                entity.Property(x => x.BackgroundColorHex)
                    .HasColumnName("background_color_hex")
                    .HasMaxLength(8);

                entity.Property(x => x.ForegroundColorHex)
                    .HasColumnName("foreground_color_hex")
                    .HasMaxLength(8);

                entity.HasOne(x => x.Node)
                    .WithOne(x => x.GroupItem)
                    .HasForeignKey<GroupItem>(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_group_item_node");
            });
        }

        private static void ConfigureFileItem(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileItem>(entity =>
            {
                entity.ToTable("file_item");

                entity.HasKey(x => x.NodeId)
                    .HasName("PK_file_item");

                entity.Property(x => x.NodeId)
                    .HasColumnName("node_id");

                entity.Property(x => x.ContentHash)
                    .HasColumnName("content_hash")
                    .HasMaxLength(64);

                entity.Property(x => x.OriginalFilename)
                    .HasColumnName("original_filename")
                    .HasMaxLength(255);

                entity.Property(x => x.Notes)
                    .HasColumnName("notes");

                entity.Property(x => x.CreatedAtUtc)
                    .HasColumnName("created_at_utc")
                    .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                    .HasColumnName("updated_at_utc")
                    .IsRequired();

                entity.HasOne(x => x.Node)
                    .WithOne(x => x.FileItem)
                    .HasForeignKey<FileItem>(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_file_item_node");

                entity.HasOne(x => x.Content)
                    .WithMany(x => x.FileItems)
                    .HasForeignKey(x => x.ContentHash)
                    .HasPrincipalKey(x => x.Hash)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_file_item_content");

                entity.HasIndex(x => x.ContentHash)
                    .HasDatabaseName("IX_file_item_content_hash");
            });
        }

        private static void ConfigureFileContent(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FileContent>(entity =>
            {
                entity.ToTable("file_content");

                entity.HasKey(x => x.Hash)
                    .HasName("PK_file_content");

                entity.Property(x => x.Hash)
                    .HasColumnName("hash")
                    .HasMaxLength(64)
                    .IsRequired();

                //entity.Property(x => x.HashAlgorithm)
                //    .HasColumnName("hash_algorithm")
                //    .HasMaxLength(32)
                //    .HasDefaultValue("SHA256")
                //    .IsRequired();

                entity.Property(x => x.SizeBytes)
                    .HasColumnName("size_bytes")
                    .IsRequired();

                entity.Property(x => x.MimeType)
                    .HasColumnName("mime_type")
                    .HasMaxLength(100);

                entity.Property(x => x.DurationMs)
                    .HasColumnName("duration_ms");

                entity.Property(x => x.Width)
                    .HasColumnName("width");

                entity.Property(x => x.Height)
                    .HasColumnName("height");

                entity.Property(x => x.CreatedAtUtc)
                    .HasColumnName("created_at_utc")
                    .IsRequired();

                entity.HasIndex(x => x.SizeBytes)
                    .HasDatabaseName("IX_file_content_size_bytes");
            });
        }

        private static void ConfigureNodeAlias(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NodeAlias>(entity =>
            {
                entity.ToTable("node_alias");

                entity.HasKey(x => x.Id)
                    .HasName("PK_node_alias");

                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.NodeId)
                    .HasColumnName("node_id")
                    .IsRequired();

                entity.Property(x => x.Alias)
                    .HasColumnName("alias")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.HasOne(x => x.Node)
                    .WithMany(x => x.Aliases)
                    .HasForeignKey(x => x.NodeId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_node_alias_node");

                entity.HasIndex(x => new { x.NodeId, x.Alias })
                    .IsUnique()
                    .HasDatabaseName("UQ_node_alias_node_alias");

                entity.HasIndex(x => x.Alias)
                    .HasDatabaseName("IX_node_alias_alias");
            });
        }

        private static void ConfigureTag(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.ToTable("tag");

                entity.HasKey(x => x.Id)
                    .HasName("PK_tag");

                entity.HasAlternateKey(x => new { x.Id, x.UserId })
                    .HasName("UQ_tag_id_user");

                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

                entity.Property(x => x.Name)
                    .HasColumnName("name")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.Description)
                    .HasColumnName("description");

                entity.Property(x => x.BackgroundColorHex)
                    .HasColumnName("background_color_hex")
                    .HasMaxLength(8);

                entity.Property(x => x.ForegroundColorHex)
                    .HasColumnName("foreground_color_hex")
                    .HasMaxLength(8);

                entity.Property(x => x.CreatedAtUtc)
                    .HasColumnName("created_at_utc")
                    .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                    .HasColumnName("updated_at_utc")
                    .IsRequired();

                entity.HasOne(x => x.User)
                    .WithMany(x => x.Tags)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_tag_user");

                entity.HasIndex(x => x.UserId)
                    .HasDatabaseName("IX_tag_user_id");

                entity.HasIndex(x => new { x.UserId, x.Name })
                    .IsUnique()
                    .HasDatabaseName("UQ_tag_user_name");
            });
        }

        private static void ConfigureTagAlias(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TagAlias>(entity =>
            {
                entity.ToTable("tag_alias");

                entity.HasKey(x => x.Id)
                    .HasName("PK_tag_alias");

                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.TagId)
                    .HasColumnName("tag_id")
                    .IsRequired();

                entity.Property(x => x.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

                entity.Property(x => x.Alias)
                    .HasColumnName("alias")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.HasOne(x => x.Tag)
                    .WithMany(x => x.Aliases)
                    .HasForeignKey(x => new { x.TagId, x.UserId })
                    .HasPrincipalKey(x => new { x.Id, x.UserId })
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_tag_alias_tag");

                entity.HasIndex(x => new { x.UserId, x.Alias })
                    .IsUnique()
                    .HasDatabaseName("UQ_tag_alias_user_alias");
            });
        }

        private static void ConfigureNodeTag(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NodeTag>(entity =>
            {
                entity.ToTable("node_tag");

                entity.HasKey(x => new { x.NodeId, x.TagId })
                    .HasName("PK_node_tag");

                entity.Property(x => x.NodeId)
                    .HasColumnName("node_id")
                    .IsRequired();

                entity.Property(x => x.TagId)
                    .HasColumnName("tag_id")
                    .IsRequired();

                entity.Property(x => x.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .HasColumnName("created_at_utc")
                    .IsRequired();

                entity.HasOne(x => x.Node)
                    .WithMany(x => x.NodeTags)
                    .HasForeignKey(x => new { x.NodeId, x.UserId })
                    .HasPrincipalKey(x => new { x.Id, x.UserId })
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_node_tag_node");

                entity.HasOne(x => x.Tag)
                    .WithMany(x => x.NodeTags)
                    .HasForeignKey(x => new { x.TagId, x.UserId })
                    .HasPrincipalKey(x => new { x.Id, x.UserId })
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_node_tag_tag");

                entity.HasIndex(x => x.NodeId)
                    .HasDatabaseName("IX_node_tag_node_id");

                entity.HasIndex(x => x.TagId)
                    .HasDatabaseName("IX_node_tag_tag_id");

                entity.HasIndex(x => new { x.UserId, x.TagId })
                    .HasDatabaseName("IX_node_tag_user_tag");
            });
        }

        private static void ConfigureCloudProviderAccount(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CloudProviderAccount>(entity =>
            {
                entity.ToTable("cloud_provider_account");

                entity.HasKey(x => x.Id)
                    .HasName("PK_cloud_provider_account");

                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

                entity.Property(x => x.Provider)
                    .HasColumnName("provider")
                    .HasConversion<string>()
                    .HasMaxLength(32)
                    .IsRequired();

                entity.Property(x => x.ProviderAccountId)
                    .HasColumnName("provider_account_id")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.DisplayName)
                    .HasColumnName("display_name")
                    .HasMaxLength(255);

                entity.Property(x => x.Email)
                    .HasColumnName("email")
                    .HasMaxLength(255);

                entity.Property(x => x.AccessTokenEncrypted)
                    .HasColumnName("access_token_encrypted")
                    .IsRequired();

                entity.Property(x => x.RefreshTokenEncrypted)
                    .HasColumnName("refresh_token_encrypted");

                entity.Property(x => x.TokenExpiresAtUtc)
                    .HasColumnName("token_expires_at_utc");

                entity.Property(x => x.Scopes)
                    .HasColumnName("scopes");

                entity.Property(x => x.IsActive)
                    .HasColumnName("is_active")
                    .HasDefaultValue(true);

                entity.Property(x => x.CreatedAtUtc)
                    .HasColumnName("created_at_utc")
                    .IsRequired();

                entity.Property(x => x.UpdatedAtUtc)
                    .HasColumnName("updated_at_utc")
                    .IsRequired();

                entity.Property(x => x.LastSyncAtUtc)
                    .HasColumnName("last_sync_at_utc");

                entity.HasOne(x => x.User)
                    .WithMany(x => x.CloudProviderAccounts)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_cloud_provider_account_user");

                entity.HasIndex(x => new { x.UserId, x.Provider, x.ProviderAccountId })
                    .IsUnique()
                    .HasDatabaseName("UQ_cloud_provider_account_user_provider_account");
            });
        }

        private static void ConfigureStorageReference(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StorageReference>(entity =>
            {
                entity.ToTable("storage_reference");

                entity.HasKey(x => x.Id)
                    .HasName("PK_storage_reference");

                entity.Property(x => x.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.UserId)
                    .HasColumnName("user_id")
                    .IsRequired();

                entity.Property(x => x.FileNodeId)
                    .HasColumnName("file_node_id");

                entity.Property(x => x.ContentHash)
                    .HasColumnName("content_hash")
                    .HasMaxLength(64);

                entity.Property(x => x.CloudProviderAccountId)
                    .HasColumnName("cloud_provider_account_id")
                    .IsRequired();

                entity.Property(x => x.Provider)
                    .HasColumnName("provider")
                    .HasConversion<string>()
                    .HasMaxLength(32)
                    .IsRequired();

                entity.Property(x => x.ProviderItemId)
                    .HasColumnName("provider_item_id")
                    .HasMaxLength(512)
                    .IsRequired();

                entity.Property(x => x.ProviderPath)
                    .HasColumnName("provider_path");

                entity.Property(x => x.Name)
                    .HasColumnName("name")
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(x => x.ProviderRevision)
                    .HasColumnName("provider_revision")
                    .HasMaxLength(255);

                entity.Property(x => x.MimeType)
                    .HasColumnName("mime_type")
                    .HasMaxLength(100);

                entity.Property(x => x.SizeBytes)
                    .HasColumnName("size_bytes");

                entity.Property(x => x.ProviderModifiedAtUtc)
                    .HasColumnName("provider_modified_at_utc");

                entity.Property(x => x.LastSeenAtUtc)
                    .HasColumnName("last_seen_at_utc")
                    .IsRequired();

                entity.Property(x => x.State)
                    .HasColumnName("state")
                    .HasConversion<string>()
                    .HasMaxLength(32)
                    .IsRequired();

                entity.Property(x => x.MetadataJson)
                    .HasColumnName("metadata_json");

                entity.HasOne(x => x.User)
                    .WithMany()
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_storage_reference_user");

                entity.HasOne(x => x.CloudProviderAccount)
                    .WithMany(x => x.StorageReferences)
                    .HasForeignKey(x => x.CloudProviderAccountId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_storage_reference_cloud_provider_account");

                entity.HasOne(x => x.FileItem)
                    .WithMany(x => x.StorageReferences)
                    .HasForeignKey(x => x.FileNodeId)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_storage_reference_file_item");

                entity.HasOne(x => x.Content)
                    .WithMany(x => x.StorageReferences)
                    .HasForeignKey(x => x.ContentHash)
                    .HasPrincipalKey(x => x.Hash)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("FK_storage_reference_content");

                entity.HasIndex(x => x.UserId)
                    .HasDatabaseName("IX_storage_reference_user_id");

                entity.HasIndex(x => x.FileNodeId)
                    .HasDatabaseName("IX_storage_reference_file_node_id");

                entity.HasIndex(x => x.ContentHash)
                    .HasDatabaseName("IX_storage_reference_content_hash");

                entity.HasIndex(x => x.CloudProviderAccountId)
                    .HasDatabaseName("IX_storage_reference_cloud_provider_account_id");

                entity.HasIndex(x => new { x.CloudProviderAccountId, x.ProviderItemId })
                    .IsUnique()
                    .HasDatabaseName("UQ_storage_reference_provider_item");

                entity.HasIndex(x => new { x.Provider, x.ProviderItemId })
                    .HasDatabaseName("IX_storage_reference_provider_item");
            });
        }
    }
}