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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure composite keys for junction tables
            modelBuilder.Entity<ProjectMembership>()
                .HasKey(pm => new { pm.ProjectID, pm.UserID });

            modelBuilder.Entity<ProjectMetadataField>()
                .HasKey(pm => new { pm.ProjectID, pm.FieldID });

            modelBuilder.Entity<AssetMetadata>()
                .HasKey(am => new { am.BlobID, am.FieldID });

            base.OnModelCreating(modelBuilder);
        }
    }
    
}
