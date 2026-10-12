using System.Net;
using System.Net.Http.Json;
using SubscrioInstance = Subscrio.Core.Subscrio;

namespace ReleaseBoard;

public static class SelfCheck
{
    public static async Task RunAsync(string address, IServiceProvider services)
    {
        using var client = new HttpClient { BaseAddress = new Uri(address) };
        var writer = services.GetRequiredService<NotesWriter>();

        await ExpectDeniedAsync(client, "/demo/starter/notes.md");
        Require(writer.GeneratedCount == 0, "Starter denial generated a file.");
        Console.WriteLine("PASS Starter direct request: 403; files generated: 0");

        await ExpectDeniedAsync(client, "/demo/unassigned/notes.md");
        Require(writer.GeneratedCount == 0, "Unassigned customer generated a file.");
        Console.WriteLine("PASS No subscription: 403; files generated: 0");

        using (var allowed = await client.GetAsync("/demo/team/notes.md"))
        {
            Require(allowed.StatusCode == HttpStatusCode.OK, "Team request did not succeed.");
            Require(allowed.Content.Headers.ContentType?.MediaType == "text/markdown", "Wrong content type.");
            Require(allowed.Content.Headers.ContentDisposition?.DispositionType == "attachment", "Not a download.");
            var filename = allowed.Content.Headers.ContentDisposition?.FileNameStar
                ?? allowed.Content.Headers.ContentDisposition?.FileName?.Trim('"');
            Require(filename == "release-notes.md", "Wrong filename.");
            Require(await allowed.Content.ReadAsStringAsync() == "# Release notes\n\n- Added a project search box.\n", "Wrong file content.");
            Require(writer.GeneratedCount == 1, "Expected exactly one generated file.");
        }
        Console.WriteLine("PASS Team direct request: 200; release-notes.md; files generated: 1");

        using (var scope = services.CreateScope())
        {
            var subscrio = scope.ServiceProvider.GetRequiredService<SubscrioInstance>();
            await subscrio.Plans.SetFeatureValueAsync("team", "download-notes", "false");
        }
        await ExpectDeniedAsync(client, "/demo/team/notes.md");
        Require(writer.GeneratedCount == 1, "Disabled feature generated another file.");
        Console.WriteLine("PASS Team feature disabled: 403; files generated still: 1");
        Console.WriteLine("All HTTP feature-access checks passed.");
    }

    private static async Task ExpectDeniedAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        Require(response.StatusCode == HttpStatusCode.Forbidden, $"Expected 403 for {path}.");
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Require(body?.GetValueOrDefault("error") == "download_not_included", "Wrong denial body.");
        Require(response.Content.Headers.ContentDisposition is null, "Denial returned a file attachment.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
