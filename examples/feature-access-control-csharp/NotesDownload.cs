using System.Text;
using SubscrioInstance = Subscrio.Core.Subscrio;

namespace ReleaseBoard;

public static class NotesDownload
{
    public static async Task<IResult> HandleAsync(
        string customerKey, SubscrioInstance subscrio, NotesWriter writer)
    {
        var allowed = await subscrio.FeatureChecker.IsEnabledForCustomerAsync(
            customerKey, "releaseboard", "download-notes");

        if (!allowed)
            return Results.Json(
                new { error = "download_not_included" }, statusCode: 403);

        var markdown = writer.CreateMarkdown();
        return Results.File(
            Encoding.UTF8.GetBytes(markdown),
            "text/markdown; charset=utf-8",
            "release-notes.md");
    }
}

public sealed class NotesWriter
{
    private int generatedCount;
    public int GeneratedCount => Volatile.Read(ref generatedCount);

    public string CreateMarkdown()
    {
        Interlocked.Increment(ref generatedCount);
        return "# Release notes\n\n- Added a project search box.\n";
    }
}
