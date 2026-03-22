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
                repositories = await this.Repository.GetRepositoriesAsync();
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
}
