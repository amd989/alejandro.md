namespace alejandromd.Services
{
    public class RepositoryModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string Link { get; set; }
        public int StargazersCount { get; set; }
        public string SocialImageUrl { get; set; }
        public string Owner { get; set; }
        public bool IsOrganization { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
