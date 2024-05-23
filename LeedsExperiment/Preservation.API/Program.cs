using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.S3;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using Preservation;
using Preservation.API;
using Preservation.API.Data;
using Preservation.API.Models;
using Preservation.API.Services;
using Preservation.API.Services.Exporter;
using Preservation.API.Services.ImportJobs;
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
    client.Timeout = TimeSpan.FromMinutes(30); // for import-job. Obviously too high in reality
});

builder.Services
    .AddHttpContextAccessor()
    .AddHttpLogging(o => { })
    .AddScoped<UriGenerator>()
    .AddScoped<ModelConverter>()
    .AddScoped<IIdentityService, FakeIdentityService>()
    .AddScoped<DepositExporter>()
    .AddSingleton<IExportQueue, InProcessExportQueue>()
    .AddDefaultAWSOptions(builder.Configuration.GetAWSOptions())
    .AddAWSService<IAmazonS3>()
    .AddPreservationContext(builder.Configuration)
    .AddHostedService<DepositExporterService>()
    .AddSingleton<IImportService, S3ImportService>()
    .AddHostedService<ImportJobExecutorService>()
    .AddScoped<ImportJobRunner>()
    .AddSingleton<IImportJobQueue, InProcessImportJobQueue>();

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

builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

app.MapGet("/ping", () => "pong").ExcludeFromDescription();

app.TryRunMigrations(app.Configuration, app.Logger);
app.UseForwardedHeaders();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.MapControllers();
app.UseHttpLogging();
app.Run();