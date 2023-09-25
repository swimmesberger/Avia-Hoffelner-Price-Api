using dev.wimmesberger.avia.price.tracker.Avia.Contract;

namespace dev.wimmesberger.avia.price.tracker.Avia;

public sealed class AviaService : IAviaService {
    private readonly ILogger _logger;
    private readonly IAviaPdfProvider _pdfProvider;
    private readonly IAviaPdfParser _pdfParser;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AviaService(ILogger<AviaService> logger, IAviaPdfProvider pdfProvider, IAviaPdfParser pdfParser) {
        _logger = logger;
        _pdfProvider = pdfProvider;
        _pdfParser = pdfParser;
    }

    public async ValueTask<AviaData> GetAviaData(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Retrieving Avia Data");
        await using var pdfStream = await _pdfProvider.FindAndOpenPdf(cancellationToken);
        return _pdfParser.ParsePdf(pdfStream);
    }
}
