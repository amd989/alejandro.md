using alejandromd.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Caching.Distributed;
using Octokit;

namespace alejandromd.Components.Pages
{
    public partial class Home
    {
        private IEnumerable<Repository> repositories = [];

        private IEnumerable<RssItem> posts = [];

        [Inject]
        public IGitHubService Repository { get; set; }

        [Inject]
        public IRssService RssReader { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                repositories = await this.Repository.GetRepositoriesAsync();    
            }
            catch (Exception)
            {}
            posts = (await this.RssReader.ReadAsync()).Take(8);

            base.OnInitializedAsync();
        }        
    }
}
