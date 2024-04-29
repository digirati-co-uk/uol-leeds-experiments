using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Repository (Container, DigitalObject, Binary)
app.MapGet("/repository/{*path}",
    (string path, [FromRoute] string? version = null) =>
        $"Retrieve Container/DigitalObject/Binary at {path}, version {version ?? "v-default"}");

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

app.Run();