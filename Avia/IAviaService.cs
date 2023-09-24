namespace dev.wimmesberger.avia.price.tracker.Avia;

public interface IAviaService {
    ValueTask<AviaData> GetAviaData(CancellationToken cancellationToken = default);
}
