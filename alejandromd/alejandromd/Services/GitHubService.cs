using Microsoft.Extensions.Caching.Distributed;
using Octokit;

namespace alejandromd.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly Func<Task<GitHubClient>> clientFactory;

        public GitHubService(Func<Task<GitHubClient>> client)
        {
            clientFactory = client;
        }
        public async Task<IEnumerable<Repository>> GetRepositoriesAsync()
        {
            var client = await clientFactory();
            var repos = await client.Repository.GetAllForCurrent();
            return repos.OrderByDescending(r => r.StargazersCount).Take(6);
        }
    }
}
