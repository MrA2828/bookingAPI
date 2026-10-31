using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SkyWatch.Data;
using SkyWatch.Entities;

namespace SkyWatch.IntegrationTests;

// Hosts the real API against a throw-away Postgres database (created on startup by EnsureCreated, dropped on
// dispose) so the dev database is never touched. Needs the docker compose "skywatch-db" service running.
public class SkyWatchFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string UserEmail = "integration@example.com";
    public const string UserPassword = "Passw0rd!x";

    private readonly string _database = $"skywatch_test_{Guid.NewGuid():N}";

    public HttpClient Anonymous { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default",
            $"Host=localhost;Port=5543;Database={_database};Username=skywatch;Password=skywatch_dev_pw");
        builder.UseSetting("ReferenceData:Enabled", "false"); // no 40 MB download in tests
    }

    public async Task InitializeAsync()
    {
        Anonymous = CreateClient(); // starts the host, which creates the schema

        using var scope = Services.CreateScope();
        await TestData.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());

        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var created = await users.CreateAsync(
            new User { UserName = UserEmail, Email = UserEmail, EmailConfirmed = true, CreatedAt = DateTime.UtcNow }, UserPassword);
        if (!created.Succeeded) throw new InvalidOperationException(string.Join("; ", created.Errors.Select(e => e.Description)));

        var login = await Anonymous.PostAsJsonAsync("/auth/login", new { email = UserEmail, password = UserPassword });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        Client = CreateClient();
        Client.DefaultRequestHeaders.Authorization = new("Bearer", token);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using (var scope = Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureDeletedAsync();
        NpgsqlConnection.ClearAllPools();
        await base.DisposeAsync();
    }
}
