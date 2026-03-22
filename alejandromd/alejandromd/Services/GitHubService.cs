using Microsoft.Extensions.Caching.Distributed;
using Octokit;

namespace alejandromd.Services
{
    public class GitHubService : IGitHubService
    {
        private readonly Func<Task<(GitHubClient Client, string Owner)>> clientFactory;
        private readonly IDistributedCache cache;

        public GitHubService(Func<Task<(GitHubClient Client, string Owner)>> client, IDistributedCache cache)
        {
            clientFactory = client;
            this.cache = cache;
        }
        public async Task<IEnumerable<RepositoryModel>> GetRepositoriesAsync()
        {
            return await cache.GetOrCacheAsync("github:repos", async () =>
            {
                var (client, owner) = await clientFactory();
                var repos = await client.Repository.GetAllForUser(owner);
                return repos.OrderByDescending(r => r.StargazersCount).Take(6).Select(a => new RepositoryModel
                {
                    Name = a.Name,
                    Description = a.Description,
                    Language = a.Language,
                    StargazersCount = a.StargazersCount.ToString(),
                    Link = a.HtmlUrl
                }).ToList();
            }, TimeSpan.FromDays(1));
        }
    }
}
