using Amazon.S3;
using Fedora;
using Storage;
using System.Net.Http.Headers;
using System.Reflection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "UoL Demo Storage API",
        Version = "v0.1",
        Description =
            "Wrapper around Fedora API. For demo/test purposes only, focussed on 'happy path' (minimal error handling etc)."
    });
    
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    opts.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var fedoraAwsOptions = builder.Configuration.GetAWSOptions("Fedora-AWS");
builder.Services.AddDefaultAWSOptions(fedoraAwsOptions);
builder.Services.AddAWSService<IAmazonS3>();

builder.Services.AddMemoryCache();

builder.Services.Configure<StorageApiOptions>(builder.Configuration.GetSection("Storage-API"));
builder.Services.Configure<FedoraAwsOptions>(builder.Configuration.GetSection("Fedora-AWS-S3"));
var apiConfig = builder.Configuration.GetSection("Fedora-API");
builder.Services.Configure<FedoraApiOptions>(apiConfig);

builder.Services.AddSingleton<IStorageMapper, OcflS3StorageMapper>();
builder.Services.AddSingleton<IImportService, S3ImportService>();
builder.Services.AddHttpClient<IFedora, FedoraWrapper>(client =>
{
    var apiOptions = apiConfig.Get<FedoraApiOptions>();
    client.BaseAddress = new Uri(apiOptions!.ApiRoot);
    var credentials = $"{apiOptions!.AdminUser}:{apiOptions.AdminPassword}";
    var authHeader = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(credentials));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
});


var app = builder.Build();

app.MapGet("/api/ping", () => "pong").ExcludeFromDescription();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
