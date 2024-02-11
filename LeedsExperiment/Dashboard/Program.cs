using Dlcs;
using Preservation;
using PreservationApiClient;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// https://learn.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
builder.Services.AddHttpClient<IPreservation, PreservationService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseAddress"]!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var dlcsConfig = builder.Configuration.GetSection("Dlcs");
builder.Services.Configure<DlcsOptions>(dlcsConfig); 
builder.Services.AddHttpClient<IDlcs, Dlcs.SimpleDlcs.Dlcs>(client =>
{
    var dlcsOptions = dlcsConfig.Get<DlcsOptions>();
    client.BaseAddress = new Uri(dlcsOptions!.ApiEntryPoint!);
    var credentials = $"{dlcsOptions.ApiKey}:{dlcsOptions.ApiSecret}";
    var authHeader = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(credentials));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromMilliseconds(dlcsOptions.DefaultTimeoutMs);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("IIIF",
        policy =>
        {
            policy.WithOrigins("*");
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
