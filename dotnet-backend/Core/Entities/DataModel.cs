using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace DataModel
{
    // --------------------------
    // Entity Classes
    // --------------------------

    public class User
    {
        [Key]
        public int UserID { get; set; }
        
        public string Name { get; set; }
        
        public string Email { get; set; }
        
        public bool IsSuperAdmin { get; set; }
        
        public DateTime LastUpdated { get; set; }

        // Navigation properties
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<Log> Logs { get; set; } = new List<Log>();
        public virtual ICollection<ProjectMembership> ProjectMemberships { get; set; } = new List<ProjectMembership>();
    }

    public class Asset
    {
        [Key]
        public int BlobID { get; set; }
        
        public string FileName { get; set; }
        
        public string MimeType { get; set; }

        // Foreign keys
        public int ProjectID { get; set; }
        public int UserID { get; set; }
        
        // Navigation properties
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }
        
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
        
        public virtual ICollection<AssetMetadata> AssetMetadata { get; set; } = new List<AssetMetadata>();
    }

    public class Project
    {
        [Key]
        public int ProjectID { get; set; }
        
        public string Name { get; set; }
        
        public string Version { get; set; }
        
        public string Location { get; set; }
        
        public string Description { get; set; }
        
        public DateTime CreationTime { get; set; }
        
        public bool Active { get; set; }
        
        // Navigation properties
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<ProjectMembership> ProjectMemberships { get; set; } = new List<ProjectMembership>();
        public virtual ICollection<ProjectMetadataField> ProjectMetadataFields { get; set; } = new List<ProjectMetadataField>();
        
        // Each project can have one Palette
        public virtual Palette Palette { get; set; }
    }

    public class Log
    {
        [Key]
        public int LogID { get; set; }
        
        public string Action { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        // Foreign key
        public int UserID { get; set; }
        
        // Navigation property
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }

    // Junction table for many-to-many between Project and User.
    public class ProjectMembership
    {
        public int ProjectID { get; set; }
        public int UserID { get; set; }
        
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }
        
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
    }

    public class Palette
    {
        [Key]
        public int PaletteID { get; set; }
        
        // Assume each Palette belongs to a Project.
        public int ProjectID { get; set; }
        
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }
        
        // Palette can have many metadata fields.
        public virtual ICollection<MetadataField> MetadataFields { get; set; } = new List<MetadataField>();
    }

    public class MetadataField
    {
        [Key]
        public int FieldID { get; set; }
        
        public string FieldName { get; set; }
        
        public string FieldType { get; set; }
        
        // Optionally, associate a field with a Palette.
        public int? PaletteID { get; set; }
        
        [ForeignKey("PaletteID")]
        public virtual Palette Palette { get; set; }
        
        public virtual ICollection<ProjectMetadataField> ProjectMetadataFields { get; set; } = new List<ProjectMetadataField>();
        public virtual ICollection<AssetMetadata> AssetMetadata { get; set; } = new List<AssetMetadata>();
    }

    // Junction table between Project and MetadataField.
    public class ProjectMetadataField
    {
        public int ProjectID { get; set; }
        public int FieldID { get; set; }
        
        public string FieldValue { get; set; }
        
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }
        
        [ForeignKey("FieldID")]
        public virtual MetadataField MetadataField { get; set; }
    }

    // Junction table between Asset and MetadataField.
    public class AssetMetadata
    {
        public int BlobID { get; set; }
        public int FieldID { get; set; }
        
        public string FieldValue { get; set; }
        
        [ForeignKey("BlobID")]
        public virtual Asset Asset { get; set; }
        
        [ForeignKey("FieldID")]
        public virtual MetadataField MetadataField { get; set; }
    }

    // Optional: A simple Tag table.
    public class Tag
    {
        [Key]
        public int TagID { get; set; }
        
        public string Name { get; set; }
    }

    // --------------------------
    // DbContext for EF Core
    // --------------------------
    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options)
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
