using Microsoft.Extensions.Caching.Distributed;
using System.ServiceModel.Syndication;
using System.Xml;

namespace alejandromd.Services
{
    public class WordpressRssService : IRssService
    {
        private readonly string url;
        private readonly IDistributedCache cache;

        public WordpressRssService(string url, IDistributedCache cache)
        {
            this.url = url;
            this.cache = cache;
        }

        public Task<IEnumerable<RssItem>> ReadAsync()
        {
            return cache.GetOrCacheAsync("feed", () =>
            {
                var reader = XmlReader.Create(this.url);
                var feed = SyndicationFeed.Load(reader);
                reader.Close();
                var items = feed.Items.Select(item => new RssItem
                {
                    Author = item.ElementExtensions.ReadElementExtensions<string>("creator", "http://purl.org/dc/elements/1.1/").FirstOrDefault() ?? string.Empty,
                    Title = item.Title.Text,
                    Description = item.Summary.Text,
                    Link = item.Links.First().Uri.ToString(),
                    PublishDate = item.PublishDate.DateTime,
                    Views = ParseViews(item.ElementExtensions.ReadElementExtensions<string>("views", "http://alejandro.md/rss/jetpack").FirstOrDefault() ?? "0 views"),
                }).ToList();

                return Task.FromResult((IEnumerable<RssItem>)items);
            });
            
        }

        private static int ParseViews(string viewsString)
        {
            return int.TryParse(viewsString.Split(' ')[0], out int views) ? views : 0;
        }
    }
}
