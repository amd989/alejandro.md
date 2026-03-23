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
            var all = await GetAllRepositoriesAsync();
            return all.Take(6);
        }

        public async Task<IEnumerable<RepositoryModel>> GetAllRepositoriesAsync()
        {
            return await cache.GetOrCacheAsync("github:repos:all", async () =>
            {
                var (client, owner) = await clientFactory();
                var repos = await client.Repository.GetAllForUser(owner);
                return repos
                    .Where(r => !r.Fork)
                    .OrderByDescending(r => r.UpdatedAt)
                    .ThenByDescending(r => r.StargazersCount)
                    .Select(a => new RepositoryModel
                    {
                        Name = a.Name,
                        Description = a.Description,
                        Language = a.Language,
                        StargazersCount = a.StargazersCount.ToString(),
                        Link = a.HtmlUrl,
                        SocialImageUrl = $"https://opengraph.githubassets.com/1/{owner}/{a.Name}",
                        Owner = owner
                    }).ToList();
            }, TimeSpan.FromDays(1));
        }
    }
}
