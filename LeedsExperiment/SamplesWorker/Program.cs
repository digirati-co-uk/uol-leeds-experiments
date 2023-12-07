using Fedora;
using SamplesWorker;
using System.Net.Http.Headers;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHttpClient<IFedora, Preservation.FedoraWrapper>(client =>
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
