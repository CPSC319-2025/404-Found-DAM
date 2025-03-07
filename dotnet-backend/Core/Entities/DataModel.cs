using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Core.Entities
{
    // --------------------------
    // Entity Classes
    // --------------------------

    public class User
    {
        [Key]
        public int UserID { get; set; }
        
        public required string Name { get; set; }
        
        public required string Email { get; set; }
        
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

        /*
            Missing: 
            thumbnailUrl
            lastUpdated
        */
    
        public required string FileName { get; set; }
        
        public required string MimeType { get; set; }

        // Foreign keys
        public int? ProjectID { get; set; }
        public int? UserID { get; set; }
        
        // Navigation properties
        [ForeignKey("ProjectID")]
        public virtual Project? Project { get; set; }
        
        // TODO reveert
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }
        
        public virtual ICollection<AssetMetadata> AssetMetadata { get; set; } = new List<AssetMetadata>();
        public virtual ICollection<AssetTag> AssetTags { get; set; } = new List<AssetTag>();
    }

    public class Project
    {
        [Key]
        public int ProjectID { get; set; }
        
        public required string Name { get; set; }
        
        public required string Version { get; set; }
        
        public required string Location { get; set; }
        
        public required string Description { get; set; }
        
        public DateTime CreationTime { get; set; }
        
        public bool Active { get; set; }
        
        // Navigation properties
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<ProjectMembership> ProjectMemberships { get; set; } = new List<ProjectMembership>();
        public virtual ICollection<ProjectMetadataField> ProjectMetadataFields { get; set; } = new List<ProjectMetadataField>();

        public virtual ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
        
        // Each project can have one Palette ??
        public virtual Palette Palette { get; set; }
    }

    public class Log
    {
        [Key]
        public int LogID { get; set; }
        
        public required string Action { get; set; }
        
        public DateTime Timestamp { get; set; }
        
        // Foreign key
        public int UserID { get; set; }
        
        // Navigation property
        [ForeignKey("UserID")]
        public required virtual User User { get; set; }
    }

    // Junction table for many-to-many between Project and User.
    public class ProjectMembership
    {
        public int ProjectID { get; set; }
        public int UserID { get; set; }
        
        [ForeignKey("ProjectID")]
        public required virtual Project Project { get; set; }
        
        [ForeignKey("UserID")]
        public required virtual User User { get; set; }
    }

    public class Palette
    {
        [Key]
        public int PaletteID { get; set; }
        
        // Assume each Palette belongs to a Project.
        public int ProjectID { get; set; }
        
        // Why project has palette ??
        [ForeignKey("ProjectID")]
        public virtual Project Project { get; set; }
        
        // Palette can have many metadata fields.
        public virtual ICollection<MetadataField> MetadataFields { get; set; } = new List<MetadataField>();
    }

    public class MetadataField
    {
        [Key]
        public int FieldID { get; set; }
        
        public required string FieldName { get; set; }
        
        public required string FieldType { get; set; }
        
        // Optionally, associate a field with a Palette.
        public int? PaletteID { get; set; }
        
        // Why metadata has palette ??
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
        
        public required string FieldValue { get; set; }
        
        [ForeignKey("ProjectID")]
        public required virtual Project Project { get; set; }
        
        [ForeignKey("FieldID")]
        public required virtual MetadataField MetadataField { get; set; }
    }

    // Junction table between Asset and MetadataField.
    public class AssetMetadata
    {
        public int BlobID { get; set; }
        public int FieldID { get; set; }
        
        public required string FieldValue { get; set; }
        
        [ForeignKey("BlobID")]
        public required virtual Asset Asset { get; set; }
        
        [ForeignKey("FieldID")]
        public required virtual MetadataField MetadataField { get; set; }
    }

    public class ProjectTag 
    {
        [Key]
        public int ProjectID { get; set; }
        [Key]
        public int TagID { get; set; }


        [ForeignKey("ProjectID")]
        public required virtual Project Project { get; set; }

        [ForeignKey("TagID")]
        public required virtual Tag Tag { get; set; }

    }

    public class AssetTag
    {
        [Key]
        public int BlobID { get; set; }
        [Key]
        public int TagID { get; set; }
        
        [ForeignKey("BlobID")]
        public required virtual Asset Asset { get; set; }

        [ForeignKey("TagID")]
        public required virtual Tag Tag { get; set; }

    }

    // Optional: A simple Tag table.
    public class Tag
    {
        [Key]
        public int TagID { get; set; }
        
        public required string Name { get; set; }

        public virtual ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
        public virtual ICollection<AssetTag> AssetTags { get; set; } = new List<AssetTag>();
    }

}
