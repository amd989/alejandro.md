namespace alejandromd.Services
{
    public record RssItem
    {
        public string Title { get; init; }
        public string Description { get; init; }
        public string Link { get; init; }
        public string Author { get; init; }
        public DateTime PublishDate { get; init; }
        public int Views { get; set; }
    }
}