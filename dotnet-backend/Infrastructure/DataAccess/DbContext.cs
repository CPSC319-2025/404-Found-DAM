using Microsoft.EntityFrameworkCore;
using Core.Entities;

namespace Infrastructure.DataAccess {
    public class DAMDbContext : DbContext {
        public DAMDbContext(DbContextOptions<DAMDbContext> options)
            : base(options)
        { }

        public DbSet<User> Users { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<ProjectMembership> ProjectMemberships { get; set; }
        public DbSet<Palette> Palettes { get; set; }
        public DbSet<MetadataField> MetadataFields { get; set; }
        public DbSet<ProjectMetadataField> ProjectMetadataFields { get; set; }
        public DbSet<AssetMetadata> AssetMetadata { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<ProjectTag> ProjectTags { get; set; }
        public DbSet<AssetTag> AssetTags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure composite keys for junction tables
            modelBuilder.Entity<ProjectMembership>()
                .HasKey(pm => new { pm.ProjectID, pm.UserID });

            modelBuilder.Entity<ProjectMetadataField>()
                .HasKey(pm => new { pm.ProjectID, pm.FieldID });

            modelBuilder.Entity<AssetMetadata>()
                .HasKey(am => new { am.BlobID, am.FieldID });
            
            modelBuilder.Entity<ProjectTag>()
                .HasKey(pt => new { pt.ProjectID, pt.TagID });

            modelBuilder.Entity<AssetTag>()
                .HasKey(at => new { at.BlobID, at.TagID });
            
            modelBuilder.Entity<Project>()
                .HasIndex(p => p.Name);

            modelBuilder.Entity<Tag>()
                .HasIndex(t => t.Name);

            modelBuilder.Entity<ProjectTag>()
                .HasIndex(pt => new { pt.ProjectID, pt.TagID });

            modelBuilder.Entity<AssetTag>()
                .HasIndex(at => new { at.BlobID, at.TagID });

            base.OnModelCreating(modelBuilder);
        }
    }
    
}
