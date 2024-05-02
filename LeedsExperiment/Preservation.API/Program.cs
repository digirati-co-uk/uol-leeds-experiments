using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.S3;
using Microsoft.OpenApi.Models;
using Preservation;
using Preservation.API;
using Preservation.API.Data;
using Preservation.API.Models;
using PreservationApiClient;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    });

builder.Services.Configure<PreservationSettings>(builder.Configuration);
var preservationConfig = builder.Configuration.Get<PreservationSettings>()!;

builder.Services.AddHttpClient<IPreservation, StorageService>(client =>
{
    client.BaseAddress = preservationConfig.StorageApiBaseAddress;
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services
    .AddHttpContextAccessor()
    .AddHttpLogging(o => { })
    .AddScoped<UriGenerator>()
    .AddScoped<ModelConverter>()
    .AddDefaultAWSOptions(builder.Configuration.GetAWSOptions())
    .AddAWSService<IAmazonS3>()
    .AddPreservationContext(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UoL Demo Preservation API",
        Version = "v0.1",
        Description =
            "Preservation API is consumed by the applications we are going to build. For demo/test purposes only, focussed on 'happy path' (minimal error handling etc)."
    });
    
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opts.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

app.MapGet("/ping", () => "pong");

// Deposits
app.MapPost("/deposits/export", () => "Export specified 'digitalObject' to S3");

// ImportJob
app.MapGet("/deposits/{id}/importJobs/diff", (string id) => $"Get importJob JSON for files in Deposit {id}");
app.MapPost("/deposits/{id}/importJobs", (string id) => "Import data into Fedora");

app.TryRunMigrations(app.Configuration, app.Logger);
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();
app.UseHttpLogging();
app.Run();