namespace dev.wimmesberger.avia.price.tracker.Avia.Contract;

public interface IAviaPdfProvider {
    ValueTask<Stream> FindAndOpenPdf(CancellationToken cancellationToken = default);
    ValueTask<Uri?> FindPdfLink(CancellationToken cancellationToken = default);
}