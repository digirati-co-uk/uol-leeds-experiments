using Fedora;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IFedora, Preservation.Fedora>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["FedoraApiRoot"]!);
    var authHeader = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(
               $"{builder.Configuration["FedoraAdminUser"]}:{builder.Configuration["FedoraAdminPassword"]}"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
    client.DefaultRequestHeaders.Add("Accept", "application/ld+json");
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
