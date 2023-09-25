namespace dev.wimmesberger.avia.price.tracker.Avia;

public sealed record AviaConfiguration {
    public string FetchUrl { get; init; } = string.Empty;
    public string PdfXPath { get; init; } = string.Empty;
    public AviaParsingConfiguration Parsing { get; init; } = new AviaParsingConfiguration();
}

public sealed record AviaParsingConfiguration {
    // pdfs start from bottom (top = largest y)
    public int MaximumYCoord { get; init; } = -1;
    public int MinimumYCoordDelta { get; init; } = -1;
}