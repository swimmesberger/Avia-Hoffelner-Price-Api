namespace dev.wimmesberger.avia.price.tracker.Avia;

public sealed record AviaConfiguration {
    public string FetchUrl { get; init; } = string.Empty;
    public string PdfXPath { get; init; } = string.Empty;
    public string Culture { get; init; } = string.Empty;
}
