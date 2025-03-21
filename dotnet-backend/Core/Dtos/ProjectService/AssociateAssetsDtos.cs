namespace Core.Dtos
{
    public class AssociateAssetsReq
    {
        public List<string> blobIDs { get; set; }
    }

    public class AssociateAssetsRes
    {
        public int projectID {get; set; }
        public List<string> success {get; set; }
        public List<string> fail {get; set; }

    }
}