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

        /// <summary>
        /// Most recently updated personal and organization repositories (used on the home page).
        /// </summary>
        public async Task<IEnumerable<RepositoryModel>> GetRepositoriesAsync()
        {
            var all = await GetCachedRepositoriesAsync();
            return all
                .OrderByDescending(r => r.UpdatedAt)
                .ThenByDescending(r => r.StargazersCount)
                .Take(6);
        }

        /// <summary>
        /// Personal and organization repositories, ordered by star count.
        /// </summary>
        public async Task<IEnumerable<RepositoryModel>> GetAllRepositoriesAsync()
        {
            var all = await GetCachedRepositoriesAsync();
            return all
                .OrderByDescending(r => r.StargazersCount)
                .ThenByDescending(r => r.UpdatedAt);
        }

        private Task<List<RepositoryModel>> GetCachedRepositoriesAsync()
        {
            return cache.GetOrCacheAsync("github:repos:all:v3", async () =>
            {
                var (client, owner) = await clientFactory();

                var userRepos = await client.Repository.GetAllForUser(owner);

                // Public organization memberships of the owner
                var orgs = await client.Organization.GetAllForUser(owner);
                var orgRepos = await Task.WhenAll(orgs.Select(o => client.Repository.GetAllForOrg(o.Login)));

                return userRepos
                    .Concat(orgRepos.SelectMany(r => r))
                    .Where(r => !r.Fork && !r.Private)
                    .DistinctBy(r => r.FullName)
                    .Select(r => new RepositoryModel
                    {
                        Name = r.Name,
                        Description = r.Description,
                        Language = r.Language,
                        StargazersCount = r.StargazersCount,
                        Link = r.HtmlUrl,
                        SocialImageUrl = Endpoints.OpenGraphImageEndpoint.GetUrl(r.Owner.Login, r.Name),
                        Owner = r.Owner.Login,
                        IsOrganization = !string.Equals(r.Owner.Login, owner, StringComparison.OrdinalIgnoreCase),
                        UpdatedAt = r.UpdatedAt
                    }).ToList();
            }, TimeSpan.FromDays(1));
        }
    }
}
