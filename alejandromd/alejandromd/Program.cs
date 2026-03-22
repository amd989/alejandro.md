using alejandromd.Components;
using alejandromd.Services;
using DistributedCachePollyDecorator.Policies;
using GitHubAuth;
using GitHubAuth.Jwt;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Octokit;
using StackExchange.Redis;

namespace alejandromd
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddUserSecrets<Program>().AddEnvironmentVariables();

            builder.Logging.AddConfiguration(builder.Configuration).AddConsole();

            // Configure forwarded headers for reverse proxy support
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection("GitHub"));

            // GitHub App JWT authenticator
            builder.Services.AddTransient<IAuthenticator>(sp => {
                var options = sp.GetRequiredService<IOptionsSnapshot<GitHubOptions>>().Value;
                var httpClient = sp.GetRequiredService<IHttpClientFactory>();
                var jwt = new GitHubJwtWithRS256(options.PrivateKey, options.AppId);
                var authenticator = new AppAuthenticator(jwt)
                {
                    GetClient = () => httpClient.CreateClient("GitHub")
                };
                return authenticator;
            });

            builder.Services.AddHttpClient("GitHub", (sp, client) =>
            {
                client.BaseAddress = GitHubClient.GitHubApiUrl;
            });

            // App-level GitHubClient authenticated with JWT (used to create installation tokens)
            builder.Services.AddTransient(sp =>
            {
                var options = sp.GetRequiredService<IOptionsSnapshot<GitHubOptions>>().Value;
                var authenticator = sp.GetRequiredService<IAuthenticator>();
                var authData = authenticator.GetToken();

                return new GitHubClient(new ProductHeaderValue(options.AppName))
                {
                    Credentials = new Credentials(authData.Token, Enum.Parse<AuthenticationType>(authData.TokenType.Mode))
                };
            });

            // Factory that creates an installation-authenticated GitHubClient (no user OAuth needed)
            builder.Services.AddTransient(sp =>
            {
                var options = sp.GetRequiredService<IOptionsSnapshot<GitHubOptions>>().Value;
                var logging = sp.GetRequiredService<ILogger<GitHubClient>>();

                return new Func<Task<(GitHubClient Client, string Owner)>>(async () =>
                {
                    var appClient = sp.GetRequiredService<GitHubClient>();

                    // Get the first installation of this app (your account)
                    var installations = await appClient.GitHubApps.GetAllInstallationsForCurrent();
                    if (installations.Count == 0)
                    {
                        logging.LogError("No GitHub App installations found. Install the app on your account first.");
                        return (appClient, string.Empty);
                    }

                    var installation = installations[0];

                    // Create a short-lived installation access token (server-to-server, no user involvement)
                    var tokenResponse = await appClient.GitHubApps.CreateInstallationToken(installation.Id);

                    var client = new GitHubClient(new ProductHeaderValue($"{options.AppName}-installation"))
                    {
                        Credentials = new Credentials(tokenResponse.Token)
                    };

                    return (client, installation.Account.Login);
                });
            });

            builder.Services.AddTransient<IRssService>(sp =>
            new WordpressRssService(
                builder.Configuration.GetConnectionString("blog")!,
                sp.GetRequiredService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>()));

            builder.Services.AddTransient<IGitHubService, GitHubService>();

            builder.Services.AddMemoryCache(mo => { mo.TrackStatistics = true; });

            var redis = builder.Configuration.GetConnectionString("local");
            if (!string.IsNullOrWhiteSpace(redis))
            {
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redis;
                    options.InstanceName = "alejandromd";
                });
                builder.Services.AddDataProtection().PersistKeysToStackExchangeRedis(ConnectionMultiplexer.Connect(redis));
            }

            // Decorate the inferface with a simple circuit breaker
            builder.Services.AddDistributedCacheDecorator(new CircuitBreakerSettings()
            {
                DurationOfBreak = TimeSpan.FromMinutes(5),
                ExceptionsAllowedBeforeBreaking = 1
            });

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseForwardedHeaders();
            app.UseHttpsRedirection();

            app.MapStaticAssets();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            app.Run();
        }
    }
}
