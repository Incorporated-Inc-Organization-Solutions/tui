using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MariaDb;
using tui.Contracts;
using tui.Data;
using Xunit;

namespace tui.IntegrationTests;

[CollectionDefinition("MariaDB integration tests", DisableParallelization = true)]
public sealed class MariaDbCollection : ICollectionFixture<MariaDbFixture>;

[Collection("MariaDB integration tests")]
public sealed class TestRecordsApiTests(MariaDbFixture database)
{
    [Fact]
    public async Task Post_testrecord_then_get_returns_the_saved_record()
    {
        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
        var name = $"integratietest-{Guid.NewGuid():N}";

        var createResponse = await client.PostAsJsonAsync("/api/test-records", new CreateTestRecordRequest { Name = name });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TestRecordResponse>();
        Assert.NotNull(created);
        Assert.Equal(name, created.Name);

        var records = await client.GetFromJsonAsync<List<TestRecordResponse>>("/api/test-records");

        Assert.NotNull(records);
        Assert.Contains(records, record => record.Id == created.Id && record.Name == name);
    }
}

public sealed class MariaDbFixture : IAsyncLifetime
{
    private readonly MariaDbContainer _container = new MariaDbBuilder("mariadb:11.7")
        .WithDatabase("tui_integration")
        .WithUsername("test_user")
        .WithPassword("test-password-123")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

public sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:MariaDb"] = connectionString
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseMySql(connectionString, new MariaDbServerVersion(new Version(11, 7, 2))));
        });
    }
}
