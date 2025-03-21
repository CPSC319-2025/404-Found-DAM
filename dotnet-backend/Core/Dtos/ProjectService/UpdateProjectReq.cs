namespace Core.Dtos

{
    public class UpdateProjectReq
    {
        public string? Location { get; set; }
        
        public List<ProjectMembershipDto>? Memberships { get; set; }
        
        public List<TagDto>? Tags { get; set; }
        
        public List<CustomMetadataDto>? CustomMetadata { get; set; }
    }
}
