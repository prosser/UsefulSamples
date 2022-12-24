namespace Rosser.StreamingAsyncApi.Controllers;

using Microsoft.AspNetCore.Mvc;

using Rosser.Contracts;

using System.Runtime.CompilerServices;

[Route("")]
public class TickerController : Controller
{
    /// <summary>
    /// Returns sample stock prices in a streaming manner, with a configurable delay between each result.
    /// </summary>
    /// <param name="delay">The number of milliseconds between each item</param>
    /// <returns></returns>
    [HttpGet("")]
    public async IAsyncEnumerable<StockPrice> Index([FromQuery] string[] symbols, [FromQuery] double maxDailyDelta = 0.5, [FromQuery] int delay = 50, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // vary by no more than X% a day
        double scale = delay / TimeSpan.FromDays(1).TotalMilliseconds * maxDailyDelta;

        var rand = new Random();
        Dictionary<string, decimal> prices = new();
        foreach (string symbol in symbols)
        {
            prices[symbol] = Math.Round((decimal)rand.NextDouble() * 1000, 4);
        }

        while (!ct.IsCancellationRequested)
        {
            DateTimeOffset timestamp = DateTimeOffset.UtcNow;
            foreach (string symbol in symbols)
            {
                decimal value = prices[symbol];
                double seed = rand.NextDouble();


                double change = seed > 0.5 ? 1 + seed * scale : 1 - seed * scale;
                prices[symbol] = Math.Round((decimal)change * value, 4);
                StockPrice price = new(symbol, timestamp, value);
                yield return price;
            }

            await Task.Delay(delay);
        }
    }
}
