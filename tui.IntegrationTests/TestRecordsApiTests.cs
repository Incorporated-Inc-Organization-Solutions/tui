using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MariaDb;
using tui.Contracts;
using tui.Models;
using tui.Data;
using Xunit;

namespace tui.IntegrationTests;

[CollectionDefinition("MariaDB integration tests", DisableParallelization = true)]
public sealed class MariaDbCollection : ICollectionFixture<MariaDbFixture>;

[Collection("MariaDB integration tests")]
public sealed class TestRecordsApiTests(MariaDbFixture database)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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

    [Fact]
    public async Task Post_invoice_then_get_returns_unchanged_invoice_with_open_status()
    {
        await using var factory = new ApiFactory(database.ConnectionString);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true
        });
        var suffix = Guid.NewGuid().ToString("N");
        var email = $"invoice-{suffix}@example.test";
        const string password = "Testwachtwoord-123!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Username = $"invoice{suffix[..8]}",
            Email = email,
            Password = password,
            ConfirmPassword = password
        });
        registerResponse.EnsureSuccessStatusCode();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Identifier = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var invalidResponse = await client.PostAsJsonAsync("/api/invoices", new CreateInvoiceRequest
        {
            RecipientId = 0,
            Description = string.Empty,
            TotalAmount = 0
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        var validationResponse = await invalidResponse.Content.ReadAsStringAsync();
        Assert.Contains("RecipientId", validationResponse);
        Assert.Contains("InvoiceDate", validationResponse);
        Assert.Contains("Description", validationResponse);
        Assert.Contains("TotalAmount", validationResponse);

        var recipients = await client.GetFromJsonAsync<List<RecipientResponse>>("/api/recipients");
        var recipient = Assert.Single(recipients!.Take(1));
        var request = new CreateInvoiceRequest
        {
            RecipientId = recipient.Id,
            InvoiceDate = new DateOnly(2026, 9, 30),
            Description = "Handmatig ingevoerde testfactuur",
            TotalAmount = 125.50m
        };

        var createResponse = await client.PostAsJsonAsync("/api/invoices", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<InvoiceResponse>(JsonOptions);
        Assert.NotNull(created);
        Assert.StartsWith("FAC-", created.InternalReference);
        Assert.Equal(InvoicePaymentStatus.Openstaand, created.PaymentStatus);

        var reopened = await client.GetFromJsonAsync<InvoiceResponse>(
            $"/api/invoices/{created.InternalReference}",
            JsonOptions);

        Assert.NotNull(reopened);
        Assert.Equal(request.RecipientId, reopened.RecipientId);
        Assert.Equal(request.InvoiceDate, reopened.InvoiceDate);
        Assert.Equal(request.Description, reopened.Description);
        Assert.Equal(request.TotalAmount, reopened.TotalAmount);
        Assert.Equal(InvoicePaymentStatus.Openstaand, reopened.PaymentStatus);
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
