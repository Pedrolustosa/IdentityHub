using System.Data.Common;
using IdentityHub.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityHub.API.Tests;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private DbConnection? _connection;
    private readonly IReadOnlyDictionary<string, string?> _additionalConfiguration;

    public TestWebApplicationFactory()
        : this(new Dictionary<string, string?>
        {
            ["RateLimiting:Auth:Login:PermitLimit"] = "1000",
            ["RateLimiting:Auth:Login:WindowSeconds"] = "60",
            ["RateLimiting:Auth:ForgotPassword:PermitLimit"] = "1000",
            ["RateLimiting:Auth:ForgotPassword:WindowSeconds"] = "300",
            ["RateLimiting:Auth:ResendConfirmation:PermitLimit"] = "1000",
            ["RateLimiting:Auth:ResendConfirmation:WindowSeconds"] = "300"
        })
    {
    }

    internal TestWebApplicationFactory(IReadOnlyDictionary<string, string?>? additionalConfiguration = null)
    {
        _additionalConfiguration = additionalConfiguration ?? new Dictionary<string, string?>();

        Environment.SetEnvironmentVariable("Jwt__Key", "integration-tests-jwt-key-with-at-least-32-bytes");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "IdentityHub");
        Environment.SetEnvironmentVariable("Jwt__Audience", "IdentityHubUsers");
        Environment.SetEnvironmentVariable("Jwt__ExpireMinutes", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "integration-tests-jwt-key-with-at-least-32-bytes",
                ["Jwt:Issuer"] = "IdentityHub",
                ["Jwt:Audience"] = "IdentityHubUsers",
                ["Jwt:ExpireMinutes"] = "60",
                ["Frontend:BaseUrl"] = "http://localhost:4200"
            };

            foreach (var item in _additionalConfiguration)
                settings[item.Key] = item.Value;

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbConnection>();

            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            services.AddSingleton(_connection);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite((SqliteConnection)_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection?.Dispose();
            _connection = null;

            Environment.SetEnvironmentVariable("Jwt__Key", null);
            Environment.SetEnvironmentVariable("Jwt__Issuer", null);
            Environment.SetEnvironmentVariable("Jwt__Audience", null);
            Environment.SetEnvironmentVariable("Jwt__ExpireMinutes", null);
        }
    }
}
