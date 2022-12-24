namespace Rosser.Contracts;

/// <summary>A sample data item</summary>
/// <param name="Symbol">Stock ticker symbol</param>
/// <param name="Timestamp">Timestamp of the stock price</param>
/// <param name="Value">Value of the stock at <paramref name="Timestamp"/></param>
public record StockPrice(string Symbol, DateTimeOffset Timestamp, decimal Value);