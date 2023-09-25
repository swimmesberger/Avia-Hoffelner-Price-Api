namespace dev.wimmesberger.avia.price.tracker.Avia.Contract;

public interface IAviaService {
    ValueTask<AviaData> GetAviaData(CancellationToken cancellationToken = default);
}
