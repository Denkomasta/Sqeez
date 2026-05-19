using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Sqeez.Api.Data;
using Sqeez.Api.DTOs;
using Sqeez.Api.Services.Interfaces;
using Testcontainers.PostgreSql;

namespace Sqeez.Api.Tests.Integration.Postgres
{
    [CollectionDefinition(CollectionName)]
    public class PostgresIntegrationTestCollection : ICollectionFixture<PostgresIntegrationTestFixture>
    {
        public const string CollectionName = "Postgres integration tests";
    }

    public sealed class PostgresIntegrationTestFixture : IAsyncLifetime
    {
        internal const string TokenKey = "live-integration-test-token-key-with-at-least-sixty-four-bytes-1234567890";

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("sqeez_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        public PostgresWebApplicationFactory Factory { get; private set; } = null!;

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            var connectionString = _postgres.GetConnectionString();
            Environment.SetEnvironmentVariable("TokenKey", TokenKey);
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionString);
            Environment.SetEnvironmentVariable("FrontendUrl", "http://localhost:3000");
            Environment.SetEnvironmentVariable("SUPER_USER_EMAIL", "super@sqeez.test");

            Factory = new PostgresWebApplicationFactory(connectionString);
            await ResetDatabaseAsync();
        }

        public async Task DisposeAsync()
        {
            await Factory.DisposeAsync();
            await _postgres.DisposeAsync();
        }

        public async Task ResetDatabaseAsync()
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqeezDbContext>();

            await dbContext.Database.EnsureDeletedAsync();
            await dbContext.Database.MigrateAsync();

            var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
            cache.Remove("GlobalSystemConfig");
        }
    }

    public sealed class PostgresWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public PostgresWebApplicationFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration(configurationBuilder =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TokenKey"] = PostgresIntegrationTestFixture.TokenKey,
                    ["ConnectionStrings:DefaultConnection"] = _connectionString,
                    ["FrontendUrl"] = "http://localhost:3000",
                    ["SUPER_USER_EMAIL"] = "super@sqeez.test"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<SqeezDbContext>>();
                services.AddDbContext<SqeezDbContext>(options =>
                    options.UseNpgsql(_connectionString));

                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService, NoopEmailService>();

                services.RemoveAll<IFileStorageService>();
                services.AddSingleton<IFileStorageService, NoopFileStorageService>();
            });
        }
    }

    internal sealed class NoopEmailService : IEmailService
    {
        public Task SendVerificationEmailAsync(string email, string verificationLink) => Task.CompletedTask;

        public Task SendVerificationEmailAsync(string email, string verificationLink, string? language) => Task.CompletedTask;

        public Task SendPasswordResetEmailAsync(string email, string resetLink) => Task.CompletedTask;

        public Task SendPasswordResetEmailAsync(string email, string resetLink, string? language) => Task.CompletedTask;
    }

    internal sealed class NoopFileStorageService : IFileStorageService
    {
        public Task<ServiceResult<string>> UploadFileAsync(IFormFile file, string subDirectory = "media", bool isPublic = false) =>
            Task.FromResult(ServiceResult<string>.Ok($"/test-storage/{file.FileName}"));

        public Task<ServiceResult<bool>> DeleteFileAsync(string fileUrl) =>
            Task.FromResult(ServiceResult<bool>.Ok(true));

        public Task<ServiceResult<string>> GetPhysicalFilePathAsync(string fileUrl) =>
            Task.FromResult(ServiceResult<string>.Failure("File storage is not enabled in live database tests.", Enums.ServiceError.NotFound));
    }
}
