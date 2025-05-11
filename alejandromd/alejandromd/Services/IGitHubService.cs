using Octokit;

namespace alejandromd.Services
{
    public interface IGitHubService
    {
        Task<IEnumerable<Repository>> GetRepositoriesAsync();
    }
}
