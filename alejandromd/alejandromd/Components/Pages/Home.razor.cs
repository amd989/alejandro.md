using alejandromd.Services;
using Microsoft.AspNetCore.Components;

namespace alejandromd.Components.Pages
{
    public partial class Home
    {
        private IEnumerable<RepositoryModel> repositories = [];

        private IEnumerable<RssItem> posts = [];

        [Inject]
        public required IGitHubService Repository { get; set; }

        [Inject]
        public required IRssService RssReader { get; set; }

        [Inject]
        public required ILogger<Home> Logger { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                repositories = (await this.Repository.GetRepositoriesAsync()).Select(a => new RepositoryModel
                {
                    Name = a.Name,
                    Description = a.Description,
                    Language = a.Language,
                    StargazersCount = a.StargazersCount.ToString(),
                    Link = a.HtmlUrl
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to fetch GitHub repositories");
                repositories = [];
            }
            posts = (await this.RssReader.ReadAsync()).Take(8);
            base.OnInitializedAsync();
        }
    }

    public class RepositoryModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Language { get; set; }
        public string Link { get; set; }
        public string StargazersCount { get; set; }
    }
}
