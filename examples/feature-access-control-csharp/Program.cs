using ReleaseBoard;
using Subscrio.Core.Config;
using Subscrio.Core.DependencyInjection;
using Subscrio.Core.Domain.ValueObjects;
using SubscrioInstance = Subscrio.Core.Subscrio;

var verify = args.Contains("--verify");
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args.Where(arg => arg != "--verify").ToArray(),
    EnvironmentName = verify ? Environments.Development : null
});
if (!builder.Environment.IsDevelopment())
    throw new InvalidOperationException("The fixed-customer demo routes require Development mode.");

await using var database = new LocalDatabase();
await database.CreateAsync();

builder.Services.AddSubscrio(new SubscrioConfig
{
    Database = new DatabaseConfig
    {
        ConnectionString = database.ConnectionString,
        DatabaseType = DatabaseType.SqlServer
    }
});
builder.Services.AddSingleton<NotesWriter>();
builder.WebHost.UseUrls(verify ? "http://127.0.0.1:0" : "http://127.0.0.1:5086");
builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Warning);

await using var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var subscrio = scope.ServiceProvider.GetRequiredService<SubscrioInstance>();
    await subscrio.InstallSchemaAsync();
    await Catalog.SeedAsync(subscrio);
}

// Local test fixtures, not customer-selection routes for a deployed application.
app.MapGet("/demo/starter/notes.md", (SubscrioInstance subscrio, NotesWriter writer) =>
    NotesDownload.HandleAsync("starter-customer", subscrio, writer));
app.MapGet("/demo/team/notes.md", (SubscrioInstance subscrio, NotesWriter writer) =>
    NotesDownload.HandleAsync("team-customer", subscrio, writer));
app.MapGet("/demo/unassigned/notes.md", (SubscrioInstance subscrio, NotesWriter writer) =>
    NotesDownload.HandleAsync("unassigned-customer", subscrio, writer));

try
{
    if (verify)
    {
        await app.StartAsync();
        await SelfCheck.RunAsync(app.Urls.Single(), app.Services);
    }
    else
    {
        Console.WriteLine("ReleaseBoard demo: http://127.0.0.1:5086/demo/team/notes.md");
        Console.WriteLine("Fixed demo customers only. The temporary database is removed on shutdown.");
        await app.RunAsync();
    }
}
finally
{
    await app.StopAsync();
}
