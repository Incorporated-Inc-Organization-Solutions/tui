using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tui.Components;
using tui.Data;
using tui.Endpoints;
using tui.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("MariaDb");
if (string.IsNullOrWhiteSpace(connectionString))
{
    var dbHost = builder.Configuration["Db:host"];
    var dbPort = builder.Configuration["Db:port"];
    var dbName = builder.Configuration["Db:name"];
    var dbUser = builder.Configuration["Db:user"];
    var dbPassword = builder.Configuration["Db:password"];

    if (string.IsNullOrWhiteSpace(dbHost) || string.IsNullOrWhiteSpace(dbPort) ||
        string.IsNullOrWhiteSpace(dbName) || string.IsNullOrWhiteSpace(dbUser))
    {
        throw new InvalidOperationException(
            "Databaseconfiguratie ontbreekt. Stel ConnectionStrings__MariaDb of alle Db__-variabelen in.");
    }

    connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};";
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, new MariaDbServerVersion(new Version(11, 7, 2))));
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        app.Logger.LogError(exception, "Onverwerkte fout tijdens {RequestPath}", context.Request.Path);

        if (context.Request.Path.StartsWithSegments("/api"))
        {
            await Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Interne serverfout",
                detail: "De aanvraag kon niet worden verwerkt.",
                extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
                .ExecuteAsync(context);
            return;
        }

        context.Response.Redirect("/Error");
    });
});

app.UseStatusCodePages(async statusCodeContext =>
{
    var context = statusCodeContext.HttpContext;
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        await Results.Problem(
            statusCode: context.Response.StatusCode,
            title: "API-aanvraag mislukt",
            detail: "De opgevraagde API-route bestaat niet of is niet beschikbaar.",
            extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
            .ExecuteAsync(context);
        return;
    }

    if (context.Response.StatusCode == StatusCodes.Status404NotFound)
    {
        context.Response.Redirect("/not-found");
    }
});

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    database.Database.Migrate();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapApiEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public partial class Program;
