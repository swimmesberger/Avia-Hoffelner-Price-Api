using dev.wimmesberger.avia.price.tracker.Avia.Contract;

namespace dev.wimmesberger.avia.price.tracker.Avia;

public static class AviaServiceExtensions {
    public static async ValueTask<AviaDataEntry?>
        GetCurrentAviaData(this IAviaService service, CancellationToken cancellationToken = default) {
        var data = await service.GetAviaData(cancellationToken);
        var today = DateTimeOffset.UtcNow;
        return data.Entries.FirstOrDefault(e => e.DayOfMonth.Year == today.Year && e.DayOfMonth.Month == today.Month);
    }
}