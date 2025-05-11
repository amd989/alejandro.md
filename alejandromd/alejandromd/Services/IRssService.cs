namespace alejandromd.Services
{
    public interface IRssService
    {
        Task<IEnumerable<RssItem>> ReadAsync();
    }
}
