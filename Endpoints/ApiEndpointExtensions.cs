using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using tui.Contracts;
using tui.Data;
using tui.Models;
using tui.Services;

namespace tui.Endpoints;

public static class ApiEndpointExtensions
{
    public static void MapApiEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api");

        var testRecords = api.MapGroup("/test-records").WithTags("Test records");
        testRecords.MapGet("", GetTestRecords)
            .Produces<List<TestRecordResponse>>(StatusCodes.Status200OK);
        testRecords.MapPost("", CreateTestRecord)
            .Produces<TestRecordResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        var recipients = api.MapGroup("/recipients")
            .WithTags("Ontvangers")
            .RequireAuthorization();
        recipients.MapGet("", GetRecipients)
            .Produces<List<RecipientResponse>>(StatusCodes.Status200OK);

        var invoices = api.MapGroup("/invoices")
            .WithTags("Facturen")
            .RequireAuthorization();
        invoices.MapGet("", GetInvoices)
            .Produces<List<InvoiceResponse>>(StatusCodes.Status200OK);
        invoices.MapGet("/{internalReference}", GetInvoice)
            .Produces<InvoiceResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);
        invoices.MapPost("", CreateInvoice)
            .Produces<InvoiceResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        var auth = api.MapGroup("/auth").WithTags("Authenticatie");
        auth.MapPost("/register", Register)
            .Produces<AuthUserResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        auth.MapPost("/login", Login)
            .Produces<AuthUserResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        auth.MapPost("/logout", async (HttpContext httpContext) =>
            {
                await httpContext.SignOutAsync();
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization();
        auth.MapGet("/me", GetCurrentUser)
            .Produces<AuthUserResponse>(StatusCodes.Status200OK)
            .RequireAuthorization();

        var users = api.MapGroup("/users")
            .WithTags("Gebruikersbeheer")
            .RequireAuthorization(policy => policy.RequireRole(UserRole.Admin.ToString()));
        users.MapGet("", GetUsers)
            .Produces<List<UserResponse>>(StatusCodes.Status200OK);
        users.MapPut("/{id:int}/role", ChangeUserRole)
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetTestRecords(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var records = await database.TestRecords
            .AsNoTracking()
            .OrderByDescending(record => record.CreatedAt)
            .Select(record => new TestRecordResponse(record.Id, record.Name, record.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(records);
    }

    private static async Task<IResult> CreateTestRecord(
        CreateTestRecordRequest request,
        ApplicationDbContext database,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var errors = ValidationHelper.Validate(request);
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors["Name"] = ["Naam is verplicht."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var record = new TestRecord { Name = request.Name!.Trim() };
        database.TestRecords.Add(record);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            loggerFactory.CreateLogger("Api").LogError(exception, "Testrecord kon niet worden opgeslagen.");
            return DatabaseProblem();
        }

        var response = new TestRecordResponse(record.Id, record.Name, record.CreatedAt);
        return Results.Created($"/api/test-records/{record.Id}", response);
    }

    private static async Task<IResult> GetRecipients(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var recipients = await database.Recipients
            .AsNoTracking()
            .OrderBy(recipient => recipient.Name)
            .Select(recipient => new RecipientResponse(recipient.Id, recipient.Name))
            .ToListAsync(cancellationToken);

        return Results.Ok(recipients);
    }

    private static async Task<IResult> GetInvoices(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var invoices = await database.Invoices
            .AsNoTracking()
            .Include(invoice => invoice.Recipient)
            .OrderByDescending(invoice => invoice.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(invoices.Select(ToInvoiceResponse).ToList());
    }

    private static async Task<IResult> GetInvoice(
        string internalReference,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        var invoice = await database.Invoices
            .AsNoTracking()
            .Include(candidate => candidate.Recipient)
            .SingleOrDefaultAsync(candidate => candidate.InternalReference == internalReference, cancellationToken);

        return invoice is null
            ? Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Factuur niet gevonden.")
            : Results.Ok(ToInvoiceResponse(invoice));
    }

    private static async Task<IResult> CreateInvoice(
        CreateInvoiceRequest request,
        ApplicationDbContext database,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var errors = ValidationHelper.Validate(request);
        if (string.IsNullOrWhiteSpace(request.Description))
        {
            errors["Description"] = ["Omschrijving is verplicht."];
        }
        if (request.TotalAmount <= 0 || request.TotalAmount > 999_999_999.99m)
        {
            errors["TotalAmount"] = ["Totaalbedrag moet groter zijn dan 0 en past niet binnen het toegestane bereik."];
        }

        var recipient = await database.Recipients.SingleOrDefaultAsync(
            candidate => candidate.Id == request.RecipientId,
            cancellationToken);
        if (recipient is null)
        {
            errors["RecipientId"] = ["Kies een bestaande ontvanger."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var invoice = new Invoice
        {
            InternalReference = $"FAC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            RecipientId = recipient!.Id,
            Recipient = recipient,
            InvoiceDate = request.InvoiceDate!.Value,
            Description = request.Description!.Trim(),
            TotalAmount = request.TotalAmount,
            PaymentStatus = InvoicePaymentStatus.Openstaand
        };
        database.Invoices.Add(invoice);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            loggerFactory.CreateLogger("Api").LogError(exception, "Factuur kon niet worden opgeslagen.");
            return DatabaseProblem();
        }

        return Results.Created($"/api/invoices/{invoice.InternalReference}", ToInvoiceResponse(invoice));
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        ApplicationDbContext database,
        IPasswordHasher<User> passwordHasher,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        var errors = ValidationHelper.Validate(request);
        if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
        {
            errors["ConfirmPassword"] = ["Wachtwoorden komen niet overeen."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var username = request.Username!.Trim();
        var email = request.Email!.Trim().ToLowerInvariant();
        var alreadyExists = await database.Users.AnyAsync(
            user => user.Username == username || user.Email == email,
            cancellationToken);
        if (alreadyExists)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Account bestaat al",
                detail: "Deze gebruikersnaam of dit e-mailadres is al in gebruik.");
        }

        // The first account is the initial administrator. All following registrations receive the User role.
        var isFirstUser = !await database.Users.AnyAsync(cancellationToken);
        var user = new User
        {
            Username = username,
            Email = email,
            PasswordHash = string.Empty,
            Role = isFirstUser ? UserRole.Admin : UserRole.User
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password!);
        database.Users.Add(user);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            loggerFactory.CreateLogger("Api").LogError(exception, "Account kon niet worden aangemaakt.");
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Account bestaat al",
                detail: "Deze gebruikersnaam of dit e-mailadres is al in gebruik.");
        }

        return Results.Created($"/api/users/{user.Id}", ToAuthUserResponse(user));
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        HttpContext httpContext,
        ApplicationDbContext database,
        IPasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken)
    {
        var errors = ValidationHelper.Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var identifier = request.Identifier!.Trim();
        var email = identifier.ToLowerInvariant();
        var user = await database.Users.SingleOrDefaultAsync(
            candidate => candidate.Username == identifier || candidate.Email == email,
            cancellationToken);
        if (user is null || !user.IsActive ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password!) == PasswordVerificationResult.Failed)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Inloggen mislukt",
                detail: "De inloggegevens zijn ongeldig.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Cookies");
        await httpContext.SignInAsync(new ClaimsPrincipal(identity));

        return Results.Ok(ToAuthUserResponse(user));
    }

    private static async Task<IResult> GetCurrentUser(
        ClaimsPrincipal principal,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var id))
        {
            return Results.Unauthorized();
        }

        var user = await database.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
        return user is null ? Results.Unauthorized() : Results.Ok(ToAuthUserResponse(user));
    }

    private static async Task<IResult> GetUsers(ApplicationDbContext database, CancellationToken cancellationToken)
    {
        var users = await database.Users
            .AsNoTracking()
            .OrderBy(user => user.Username)
            .Select(user => new UserResponse(user.Id, user.Username, user.Email, user.Role, user.IsActive, user.CreatedAt))
            .ToListAsync(cancellationToken);

        return Results.Ok(users);
    }

    private static async Task<IResult> ChangeUserRole(
        int id,
        ChangeUserRoleRequest request,
        ClaimsPrincipal principal,
        ApplicationDbContext database,
        CancellationToken cancellationToken)
    {
        var errors = ValidationHelper.Validate(request);
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
        {
            errors["Role"] = ["Rol moet User of Admin zijn."];
        }

        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id.ToString() && role != UserRole.Admin)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Role"] = ["Een beheerder kan zijn eigen beheerdersrol niet verwijderen."]
            });
        }

        var user = await database.Users.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (user is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Gebruiker niet gevonden.");
        }

        user.Role = role;
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return DatabaseProblem();
        }

        return Results.Ok(new UserResponse(user.Id, user.Username, user.Email, user.Role, user.IsActive, user.CreatedAt));
    }

    private static AuthUserResponse ToAuthUserResponse(User user) =>
        new(user.Id, user.Username, user.Email, user.Role);

    private static InvoiceResponse ToInvoiceResponse(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.InternalReference,
            invoice.RecipientId,
            invoice.Recipient.Name,
            invoice.InvoiceDate,
            invoice.Description,
            invoice.TotalAmount,
            invoice.PaymentStatus,
            invoice.CreatedAt);

    private static IResult DatabaseProblem() => Results.Problem(
        statusCode: StatusCodes.Status500InternalServerError,
        title: "Databasefout",
        detail: "De gegevens konden niet worden verwerkt. Probeer het later opnieuw.");
}
