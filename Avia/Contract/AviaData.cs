using System.Collections.Immutable;

namespace dev.wimmesberger.avia.price.tracker.Avia.Contract;

public sealed record class AviaData {
    public IReadOnlyList<AviaDataEntry> Entries { get; init; } = ImmutableArray<AviaDataEntry>.Empty;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record class AviaDataEntry(DateOnly DayOfMonth, decimal GrossPriceCtkWH, decimal NetPriceCtkwH);