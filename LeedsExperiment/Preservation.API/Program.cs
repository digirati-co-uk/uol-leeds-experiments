using System.Text.Json;
using System.Text.Json.Serialization;
using Preservation;
using PreservationApiClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddHttpLogging(o => { });

builder.Services.AddHttpClient<IPreservation, StorageService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["StorageApiBaseAddress"]!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// TODO - swagger

var app = builder.Build();

app.MapGet("/ping", () => "pong");

// Is this required?
app.MapPost("/repository/{*path}",
    (string path) => "Create Container in Repo, via StorageApi - restricted to some users");

// Deposits
app.MapPost("/deposits", () => "Create Deposit and assign URI. Maybe empty body");
app.MapPost("/deposits/export", () => "Export specified 'digitalObject' to S3");
app.MapGet("/deposits/{id}", (string id) => $"Get details of Deposit {id}");

// ImportJob
app.MapGet("/deposits/{id}/importJobs/diff", (string id) => $"Get importJob JSON for files in Deposit {id}");
app.MapPost("/deposits/{id}/importJobs", (string id) => "Import data into Fedora");

app.UseHttpsRedirection();
app.MapControllers();
app.UseHttpLogging();
app.Run();