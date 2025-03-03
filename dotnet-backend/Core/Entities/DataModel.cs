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
        

        // Navigation properties
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<Log> Logs { get; set; } = new List<Log>();
        public virtual ICollection<ProjectMembership> ProjectMemberships { get; set; } = new List<ProjectMembership>();
    }

    public class Asset
    {
        [Key]
        public int BlobID { get; set; }

        public int CompressedID { get; set; }
        
        public string FileName { get; set; }
        
        public string MimeType { get; set; }

        public DateTime LastUpdated { get; set; }

        public int FileSize { get; set; }

        public string Version { get; set; }


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

        public string Role { get; set; }
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
        
        
        public virtual ICollection<ProjectMetadataField> ProjectMetadataFields { get; set; } = new List<ProjectMetadataField>();
        public virtual ICollection<AssetMetadata> AssetMetadata { get; set; } = new List<AssetMetadata>();
    }

    // Junction table between Project and MetadataField.
    public class ProjectMetadataField
    {
        public int ProjectID { get; set; }
        public int FieldID { get; set; }
        public bool IsEnabled { get; set; }

        
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

}
