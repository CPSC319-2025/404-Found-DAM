namespace Core.Dtos
{
    public class SubmitAssetsReq
    {
        public List<int> blobIDs { get; set; }
    }

    public class SubmitAssetsRes
    {
        public int projectID {get; set; }
        public List<int> successfulSubmissions {get; set; }
        public List<int> failedSubmissions {get; set; }
        public DateTime submittedAt { get; set; }

    }
}