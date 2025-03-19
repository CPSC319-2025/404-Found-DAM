namespace Core.Dtos
{
    public class AssociateAssetsReq
    {
        public List<int> blobIDs { get; set; }
    }

    public class AssociateAssetsRes
    {
        public int projectID {get; set; }
        public List<int> success {get; set; }
        public List<int> fail {get; set; }

    }
}