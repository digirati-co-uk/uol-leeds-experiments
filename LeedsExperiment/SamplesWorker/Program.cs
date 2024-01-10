using Amazon.S3;
using Fedora;
using Preservation;
using SamplesWorker;
using System.Net.Http.Headers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var fedoraAwsOptions = builder.Configuration.GetAWSOptions("Fedora-AWS");
builder.Services.AddDefaultAWSOptions(fedoraAwsOptions);
builder.Services.AddAWSService<IAmazonS3>();

builder.Services.AddMemoryCache();

builder.Services.Configure<PreservationApiOptions>(builder.Configuration.GetSection("Preservation-API"));
builder.Services.Configure<FedoraAwsOptions>(builder.Configuration.GetSection("Fedora-AWS-S3"));
var apiConfig = builder.Configuration.GetSection("Fedora-API");
builder.Services.Configure<FedoraApiOptions>(apiConfig);

builder.Services.AddSingleton<IStorageMapper, OcflS3StorageMapper>();
builder.Services.AddHttpClient<IFedora, FedoraWrapper>(client =>
{
    var apiOptions = apiConfig.Get<FedoraApiOptions>();
    client.BaseAddress = new Uri(apiOptions!.ApiRoot);
    var credentials = $"{apiOptions!.AdminUser}:{apiOptions.AdminPassword}";
    var authHeader = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(credentials));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
});
var host = builder.Build();
host.Run();
