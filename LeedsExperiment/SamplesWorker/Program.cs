using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using Fedora;
using Microsoft.Extensions.Configuration;
using Preservation;
using SamplesWorker;
using System.Net.Http.Headers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var fedoraAwsOptions = builder.Configuration.GetAWSOptions("Fedora-AWS");
builder.Services.AddDefaultAWSOptions(fedoraAwsOptions);
builder.Services.AddAWSService<IAmazonS3>();

builder.Services.Configure<FedoraAwsOptions>(builder.Configuration.GetSection("Fedora-AWS-S3"));

builder.Services.AddSingleton<IStorageMapper, OcflS3StorageMapper>();
builder.Services.AddHttpClient<IFedora, FedoraWrapper>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["FedoraApiRoot"]!);
    var authHeader = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(
               $"{builder.Configuration["FedoraAdminUser"]}:{builder.Configuration["FedoraAdminPassword"]}"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

    // Don't do this for calling the API - be explicit - will be a 406 if a POST or other bodied request
    // client.DefaultRequestHeaders.Add("Accept", "application/ld+json");
});
var host = builder.Build();
host.Run();
