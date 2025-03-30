namespace Core.Dtos
{
    public class TagDto
    {
        public int? TagID { get; set; }
        public string Name { get; set; }
    }

    public class CreateTagDto
    {
        public string Name { get; set; }
    }
    
}