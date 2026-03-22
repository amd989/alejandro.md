using Microsoft.Extensions.Caching.Distributed;
using Octokit;

namespace alejandromd.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly Func<Task<(GitHubClient Client, string Owner)>> clientFactory;

        public GitHubService(Func<Task<(GitHubClient Client, string Owner)>> client)
        {
            clientFactory = client;
        }
        public async Task<IEnumerable<Repository>> GetRepositoriesAsync()
        {
            var (client, owner) = await clientFactory();
            var repos = await client.Repository.GetAllForUser(owner);
            return repos.OrderByDescending(r => r.StargazersCount).Take(6);
        }
    }
}
