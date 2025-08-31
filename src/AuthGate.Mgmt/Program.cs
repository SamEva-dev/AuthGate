var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
var app = builder.Build();

app.MapGet("/", () => Results.Json(new
{
    name = "AuthGate.Mgmt",
    status = "ok",
    plane = "control",              // plan d�admin
    version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown"
}));

// 3) Probes standard
app.MapHealthChecks("/health/live");   // process vivant ?
app.MapHealthChecks("/health/ready");  // pr�t ? (d�pendances � brancher plus tard)


app.Run();
