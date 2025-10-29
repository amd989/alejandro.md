using alejandromd.Components;
using alejandromd.Misc;
using alejandromd.Services;
using DistributedCachePollyDecorator.Policies;
using GitHubAuth;
using GitHubAuth.Jwt;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
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
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

            builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection("GitHub"));
            builder.Services.AddHttpClient("GitHub", (sp, client) =>
            {
                client.BaseAddress = GitHubClient.GitHubApiUrl;
            });
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddLogging();
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

            // Add services to the container.
            builder.Services.AddTransient(sp => 
            {
                var options = sp.GetRequiredService<IOptionsSnapshot<GitHubOptions>>().Value;
                var authenticator = sp.GetRequiredService<IAuthenticator>();
                var authData = authenticator.GetToken();

                // Use the JWT as a Bearer token
                var appClient = new GitHubClient(new ProductHeaderValue(options.AppName))
                {
                    Credentials = new Credentials(authData.Token, Enum.Parse<AuthenticationType>(authData.TokenType.Mode))
                };

                return appClient;
            });

            builder.Services.AddTransient(sp =>
            {
                var options = sp.GetRequiredService<IOptionsSnapshot<GitHubOptions>>().Value;
                return new Func<string, OauthTokenRequest>(code => new OauthTokenRequest(options.ClientId, options.ClientSecret, code));
            });

            builder.Services.AddTransient(sp =>
            {
                var options = sp.GetRequiredService<IOptionsSnapshot<GitHubOptions>>().Value;
                var cache = sp.GetRequiredService<IDistributedCache>();
                var logging = sp.GetRequiredService<ILogger<GitHubClient>>();
                var func = new Func<Task<GitHubClient>>(async () =>
                {
                    var installationId = await cache.GetStringAsync(Constants.InstallationId);
                    var token = await cache.GetStringAsync(Constants.OAuthToken);

                    var installationClient = new GitHubClient(new ProductHeaderValue($"{options.AppId}-{installationId}"));
                    if (string.IsNullOrEmpty(token))
                    {
                        var refreshToken = await cache.GetStringAsync(Constants.RefreshToken);
                        if (string.IsNullOrEmpty(refreshToken))
                        {
                            return installationClient;
                        }

                        var newToken = await installationClient.Oauth.CreateAccessTokenFromRenewalToken(new OauthTokenRenewalRequest(options.ClientId, options.ClientSecret, refreshToken));
                        if (newToken.Error == null)
                        {
                            await cache.SetStringAsync(Constants.OAuthToken, newToken.AccessToken, new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.Now.AddSeconds(newToken.ExpiresIn) });
                            await cache.SetStringAsync(Constants.RefreshToken, newToken.RefreshToken, new DistributedCacheEntryOptions { AbsoluteExpiration = DateTime.Now.AddSeconds(newToken.RefreshTokenExpiresIn) });
                            token = newToken.AccessToken;
                        }
                        else
                        {
                            logging.LogError("Error refreshing token: {Error} {ErrorDescription} Url: {ErrorUri}", newToken.Error, newToken.ErrorDescription, newToken.ErrorUri);
                        }
                    }

                    // Create a new GitHubClient using the installation token as authentication
                    if(!string.IsNullOrEmpty(token))
                    {
                        installationClient.Credentials = new Credentials(token);
                    }
                    
                    return installationClient;
                });

                return func;
            });


            builder.Services.AddTransient<IRssService>(sp => 
            new WordpressRssService(
                builder.Configuration.GetConnectionString("blog")!, 
                sp.GetRequiredService<IDistributedCache>()));

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

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddControllers();
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseForwardedHeaders();
            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            //app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
