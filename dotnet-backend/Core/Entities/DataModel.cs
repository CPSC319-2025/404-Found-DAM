using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

/*
TODO:
Consider adding unique constraint to certain attributes (such as tags and metadatafields) and handle duplicate exception 
so that a project or an asset won't have duplicated tags or metadatafields. W/o this, BE can certainly checks everytime, 
but it would require going through the database since our server does not track user state. Or if frontend can check before 
calling API since endpoints for getting a project and an asset should return full metadata and tags to frontend.

Add an attribute of pHashing to asset for detecting image (stripped-away-metadata) duplication, and only remove it the same projectID/paletteID

*/


namespace Core.Entities
{
    // --------------------------
    // Entity Classes
    // --------------------------

    public class User
    {
        /*
            TODO:
            User needs LastUpdated info to be shown in api call that check user's detail
        */
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
        
        public required string FileName { get; set; }
        
        public required string MimeType { get; set; } // E.g., image/jpeg, video/mp4, etc.

        public double FileSizeInKB { get; set; }

        public DateTime LastUpdated { get; set; }

        public enum AssetStateType
        {
            UploadedToPalette,    // 0
            SubmittedToProject    // 1
        }

        public required AssetStateType assetState { get; set; }

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
        /*
            TODO:
            version type string or double? default/start at 0.0?
        */
        [Key]
        public int ProjectID { get; set; }
        
        public required string Name { get; set; }
        
        public required string Version { get; set; }
        
        public required string Location { get; set; }
        
        public required string Description { get; set; }

        public required DateTime CreationTime { get; set; }
        
        public required bool Active { get; set; } = true; // Default to true
        
        public DateTime? ArchivedAt { get; set; } 
        
        // Navigation properties
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<ProjectMembership> ProjectMemberships { get; set; } = new List<ProjectMembership>();
        public virtual ICollection<ProjectMetadataField> ProjectMetadataFields { get; set; } = new List<ProjectMetadataField>();

        public virtual ICollection<ProjectTag> ProjectTags { get; set; } = new List<ProjectTag>();
        
        // // Each project can have one Palette ??
        // public virtual Palette Palette { get; set; }
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

        // Add Enum to represent user roles
        public enum UserRoleType
        {
            Regular,    // 0
            Admin       // 1
        }

        public UserRoleType UserRole { get; set; } 

        [ForeignKey("ProjectID")]
        public required virtual Project Project { get; set; }
        
        [ForeignKey("UserID")]
        public required virtual User User { get; set; }
    }

    // public class Palette
    // {
    //     [Key]
    //     public int PaletteID { get; set; }
        
    //     // Assume each Palette belongs to a Project.
    //     public int ProjectID { get; set; }
        
    //     // Why project has palette ??
    //     [ForeignKey("ProjectID")]
    //     public virtual Project Project { get; set; }
        
    //     // Palette can have many metadata fields.
    //     public virtual ICollection<MetadataField> MetadataFields { get; set; } = new List<MetadataField>();
    // }

    // Junction table between Project and MetadataField.
    public class ProjectMetadataField
    {
        [Key]
        public int FieldID { get; set; }
    
        public int ProjectID { get; set; }
        
        [ForeignKey("ProjectID")]
        public required virtual Project Project { get; set; }
        
        public required string FieldName { get; set; }
        public enum FieldDataType
        {
            Number,    // 0
            String,    // 1
            Boolean    // 2
        }
        
        public required FieldDataType FieldType { get; set; }
        
        public bool IsEnabled { get; set; } = false;
    
        public virtual ICollection<AssetMetadata> AssetMetadata { get; set; } = new List<AssetMetadata>();
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
        public required virtual ProjectMetadataField ProjectMetadataField { get; set; }
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