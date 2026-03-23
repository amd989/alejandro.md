using alejandromd.Services;
using Microsoft.AspNetCore.Components;

namespace alejandromd.Components.Pages
{
    public partial class Projects
    {
        private IEnumerable<RepositoryModel> repositories = [];

        [Inject]
        public required IGitHubService Repository { get; set; }

        [Inject]
        public required ILogger<Projects> Logger { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                repositories = await Repository.GetAllRepositoriesAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to fetch GitHub repositories");
                repositories = [];
            }
        }
    }
}
