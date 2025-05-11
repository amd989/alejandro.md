using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Octokit;

namespace alejandromd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly IMemoryCache cache;
        private readonly GitHubClient gitHubClient;
        private readonly Func<string, OauthTokenRequest> requestFactory;

        public WebhookController(IMemoryCache cache, GitHubClient gitHubClient, Func<string, OauthTokenRequest> requestFactory)
        {
            this.cache = cache;
            this.gitHubClient = gitHubClient;
            this.requestFactory = requestFactory;
        }

        [HttpPost(Name = "Activity")]
        public async Task<IActionResult> Index([FromBody]string json)
        {
            // Deserialize the pull_request event
            var serializer = new Octokit.Internal.SimpleJsonSerializer();
            var payload = serializer.Deserialize<ActivityPayload>(json);
            this.cache.Set("installationId", payload.Installation.Id);
            return Ok();
        }

        [HttpGet]
        [Route("Authorize")]
        public async Task<IActionResult> Authorize(string code, string installation_id, string setup_action)
        {
            if (String.IsNullOrEmpty(code))
                return RedirectToPage("Home");

            var request = requestFactory(code);
            var token = await gitHubClient.Oauth.CreateAccessToken(request);
            this.cache.Set("installationId", installation_id);
            this.cache.Set("OAuthToken", token.AccessToken);

            return RedirectToPage("Home");
        }

    }
}
