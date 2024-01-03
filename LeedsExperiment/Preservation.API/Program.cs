using Amazon.S3;
using Fedora;
using Preservation;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var fedoraAwsOptions = builder.Configuration.GetAWSOptions("Fedora-AWS");
builder.Services.AddDefaultAWSOptions(fedoraAwsOptions);
builder.Services.AddAWSService<IAmazonS3>();

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


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
