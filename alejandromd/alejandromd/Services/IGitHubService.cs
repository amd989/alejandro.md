namespace alejandromd.Services
{
    public interface IGitHubService
    {
        Task<IEnumerable<RepositoryModel>> GetRepositoriesAsync();
        Task<IEnumerable<RepositoryModel>> GetAllRepositoriesAsync();
    }
}
