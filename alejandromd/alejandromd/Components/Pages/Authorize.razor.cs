using GitHubAuth;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Octokit;
using System.Text;

namespace alejandromd.Components.Pages
{
    public partial class Authorize
    {
        [Inject]
        public IDistributedCache Cache { get; set; }

        [Inject]
        public GitHubClient GitHubClient { get; set; }

        [Inject]
        public Func<string, OauthTokenRequest> RequestFactory { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public AppAuthenticator Authenticator { get; set; }

        protected override async Task OnInitializedAsync()
        {
            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            var query = QueryHelpers.ParseQuery(uri.Query);
            if (!query.TryGetValue("code", out var code) || !query.TryGetValue("installation_id", out var installationId))
            {
                this.NavigationManager.NavigateTo("/");
                return;
            }
            
            var request = RequestFactory(code.ToString());
            var token = await GitHubClient.Oauth.CreateAccessToken(request);
            await this.Cache.SetStringAsync("installationId", installationId.ToString());
            await this.Cache.SetStringAsync("OAuthToken", token.AccessToken, new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.Now.AddSeconds(token.ExpiresIn) });
            await this.Cache.SetStringAsync("RefreshToken", token.RefreshToken, new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.Now.AddSeconds(token.RefreshTokenExpiresIn) });
            this.NavigationManager.NavigateTo("/");
        }
    }
}
