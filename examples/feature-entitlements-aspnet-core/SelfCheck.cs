using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KilnBook;

public static class SelfCheck
{
    public static async Task RunAsync(string baseUrl, ReservationStore store)
    {
        using var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

        async Task<HttpResponseMessage> Book(string? identity, bool recurring)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/reservations")
            {
                Content = JsonContent.Create(new { recurring })
            };
            if (identity is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", identity);
            return await client.SendAsync(request);
        }

        async Task Expect(string label, string? identity, bool recurring, HttpStatusCode expected, string? error = null)
        {
            using var response = await Book(identity, recurring);
            if (response.StatusCode != expected)
                throw new InvalidOperationException($"{label}: expected {(int)expected}, got {(int)response.StatusCode}.");
            if (error is not null)
            {
                var body = await response.Content.ReadFromJsonAsync<JsonElement>();
                if (body.GetProperty("error").GetString() != error)
                    throw new InvalidOperationException($"{label}: unexpected error body.");
            }
            Console.WriteLine($"PASS {label}: {(int)response.StatusCode}");
        }

        await Expect("Unauthenticated request", null, false, HttpStatusCode.Unauthorized);
        await Expect("Studio viewer lacks booking permission", "studio-viewer", true, HttpStatusCode.Forbidden);
        await Expect("No subscription has zero allowance", "visitor-owner", false, HttpStatusCode.Forbidden, "no_reservation_allowance");
        await Expect("Basic recurring booking denied", "basic-owner", true, HttpStatusCode.Forbidden, "recurring_bookings_not_included");
        await Expect("Basic first reservation", "basic-owner", false, HttpStatusCode.Created);

        var competing = await Task.WhenAll(Book("basic-owner", false), Book("basic-owner", false));
        try
        {
            var statuses = competing.Select(r => (int)r.StatusCode).Order().ToArray();
            if (!statuses.SequenceEqual(new[] { 201, 409 }) || await store.CountAsync("clay-room") != 2)
                throw new InvalidOperationException("The final reservation slot was not enforced atomically.");
            Console.WriteLine("PASS Two requests for one remaining Basic slot: 201 + 409; stored count 2");
        }
        finally { foreach (var response in competing) response.Dispose(); }

        await Expect("Basic full capacity", "basic-owner", false, HttpStatusCode.Conflict, "reservation_limit_reached");
        await Expect("Studio recurring booking allowed", "studio-owner", true, HttpStatusCode.Created);
        for (var i = 1; i < 8; i++)
        {
            using var response = await Book("studio-owner", false);
            if (response.StatusCode != HttpStatusCode.Created)
                throw new InvalidOperationException("Studio should permit eight active reservations.");
        }
        await Expect("Studio ninth reservation denied", "studio-owner", false, HttpStatusCode.Conflict, "reservation_limit_reached");
        if (await store.CountAsync("river-studio") != 8 || await store.CountAsync("visitor") != 0)
            throw new InvalidOperationException("Unexpected persisted reservation counts.");
        Console.WriteLine("PASS Stored counts: Basic 2, Studio 8, no-subscription account 0");
        Console.WriteLine("All HTTP and database checks passed.");
    }
}
