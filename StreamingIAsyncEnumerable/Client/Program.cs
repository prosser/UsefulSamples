namespace StreamAsyncClient;

using Contracts;

using System.Diagnostics;
using System.Text.Json;

internal class Program : IDisposable
{
    private readonly HttpClient client;
    private readonly JsonSerializerOptions serializerOptions;

    private Program()
    {
        client = new()
        {
            MaxResponseContentBufferSize = 20
        };
        serializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }

    public void Dispose()
    {
        ((IDisposable)client).Dispose();
    }

    public async IAsyncEnumerable<T?> GetItemsAsync<T>(string url)
    {
        using HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        await using Stream stream = await response.Content.ReadAsStreamAsync();

        await foreach (T? item in JsonSerializer.DeserializeAsyncEnumerable<T>(stream, serializerOptions))
        {
            yield return item;
        }
    }

    public async Task Run()
    {
        var stopwatch = Stopwatch.StartNew();

        string[] symbols = { "MSFT", "GOOG", "AAPL" };
        int delay = 1000;
        double maxDeltaPerDay = 0.5;

        await foreach (StockPrice? item in GetItemsAsync<StockPrice>($"https://localhost:7065/?{string.Join("&", symbols.Select(s => $"symbols={s}"))}&maxDeltaPerDay={maxDeltaPerDay}&delay={delay}"))
        {
            if (item is not null)
            {
                Console.WriteLine($"{stopwatch.Elapsed} @ {item.Timestamp} {item.Symbol} {item.Value}");
            }
        }
    }

    private static async Task Main()
    {
        using Program program = new();
        await program.Run();
    }
}