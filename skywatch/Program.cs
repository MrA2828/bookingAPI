using System.Text.Json;
using System.Text.Json.Serialization;
using SkyWatch.Data;
using SkyWatch.Features.Aircraft;
using SkyWatch.Features.Aircraft.Classification;
using SkyWatch.Features.Aircraft.Sources;
using SkyWatch.Features.ReferenceData.Sources;
using SkyWatch.Entities;
using SkyWatch.Repositories;
using SkyWatch.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    // Adds a "Bearer" auth scheme to the OpenAPI document so Swagger UI shows an Authorize button.
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Paste the accessToken returned by /auth/login"
        };
        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });
        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IAirlineRepository, AirlineRepository>();
builder.Services.AddScoped<ICountryRepository, CountryRepository>();
builder.Services.AddScoped<ISyncStateRepository, SyncStateRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Reference data (airports, airlines, countries) is refreshed from open datasets by a daily background job.
builder.Services.Configure<ReferenceDataOptions>(builder.Configuration.GetSection(ReferenceDataOptions.Section));
builder.Services.AddHttpClient<IReferenceDataSource, HttpReferenceDataSource>(c =>
{
    c.Timeout = TimeSpan.FromMinutes(3);
    c.DefaultRequestHeaders.UserAgent.ParseAdd("SkyWatch/1.0");
});
builder.Services.AddHostedService<ReferenceDataSyncService>();

// Live aircraft: adsb.lol first, adsb.fi as the fallback, both polled on demand and cached in memory.
// Routes (origin/destination) come from adsbdb.
builder.Services.Configure<AircraftOptions>(builder.Configuration.GetSection(AircraftOptions.Section));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<AdsbLolSource>(ConfigureAircraftClient(o => o.AdsbLolUrl));
builder.Services.AddHttpClient<AdsbFiSource>(ConfigureAircraftClient(o => o.AdsbFiUrl));
builder.Services.AddHttpClient<IFlightRouteProvider, AdsbdbFlightRouteProvider>(ConfigureAircraftClient(o => o.AdsbdbUrl));
builder.Services.AddTransient<IAircraftSource>(sp => sp.GetRequiredService<AdsbLolSource>());
builder.Services.AddTransient<IAircraftSource>(sp => sp.GetRequiredService<AdsbFiSource>());
builder.Services.AddScoped<IAircraftFeed, AircraftFeed>();
builder.Services.AddScoped<IAirlineCodes, AirlineCodes>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AircraftFeedExceptionHandler>();

static Action<IServiceProvider, HttpClient> ConfigureAircraftClient(Func<AircraftOptions, string> baseUrl) => (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<AircraftOptions>>().Value;
    client.BaseAddress = new Uri(baseUrl(options).TrimEnd('/') + "/");
    client.Timeout = options.RequestTimeout;
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SkyWatch/1.0");
};

// Every endpoint requires a signed-in user unless it opts out with [AllowAnonymous] (only /auth/* does).
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddIdentityApiEndpoints<User>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.SignIn.RequireConfirmedEmail = true;
    })
    .AddRoles<IdentityRole>()               // enables the roles / user-roles tables
    .AddEntityFrameworkStores<AppDbContext>();

// DEV ONLY: override Identity's no-op email sender so confirmation/reset links
// are logged to the console instead of silently dropped.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<IEmailSender<User>, DevEmailSender>();
}

var app = builder.Build();

// Create the schema and seed the roles. Idempotent, so safe on every startup.
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreatedAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in Roles.All)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}

// Configure the HTTP request pipeline.
app.MapOpenApi().AllowAnonymous();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/openapi/v1.json", "SkyWatch API v1");
    o.RoutePrefix = "swagger";
});

app.UseExceptionHandler();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Lets the integration tests host the app with WebApplicationFactory<Program>.
public partial class Program;
