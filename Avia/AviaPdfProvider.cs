using dev.wimmesberger.avia.price.tracker.Avia.Contract;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using System.Xml.XPath;

namespace dev.wimmesberger.avia.price.tracker.Avia;

public sealed class AviaPdfProvider : IAviaPdfProvider {
    private readonly ILogger _logger;
    private readonly Uri _relativeFetchUrl;
    private readonly HttpClient _httpClient;
    private readonly XPathExpression _pdfLinkXPath;

    public AviaPdfProvider(ILogger<AviaPdfProvider> logger, IOptions<AviaConfiguration> configurationOptions,
        HttpClient httpClient) {
        _logger = logger;
        _httpClient = httpClient;
        var configuration = configurationOptions.Value;
        if (!Uri.TryCreate(configuration.FetchUrl, UriKind.Absolute, out var fetchUrl)) {
            throw new AviaServiceException($"Failed to parse fetch url '{configuration.FetchUrl}' from configuration");
        }
        _httpClient.BaseAddress = new Uri(fetchUrl.GetLeftPart(UriPartial.Authority));
        _relativeFetchUrl = _httpClient.BaseAddress.MakeRelativeUri(fetchUrl);
        _pdfLinkXPath = XPathExpression.Compile(configuration.PdfXPath);
        _pdfLinkXPath.SetContext(new AviaXsltContext());
    }

    public async ValueTask<Stream> FindAndOpenPdf(CancellationToken cancellationToken = default) {
        var pdfDownloadUrl = await FindPdfLink(cancellationToken) ?? throw new AviaServiceException("Did not find PDF download url");
        _logger.LogInformation("Downloading PDF from {FetchUrl}", pdfDownloadUrl);
        var pdfData = await _httpClient.GetByteArrayAsync(pdfDownloadUrl, cancellationToken);
        // expose the buffer of the stream so there are downstream optimizations possible
        return new MemoryStream(pdfData, 0, pdfData.Length, true, true);
    }

    public async ValueTask<Uri?> FindPdfLink(CancellationToken cancellationToken = default) {
        var doc = new HtmlDocument();
        _logger.LogInformation("Fetching avia downloads page {FetchUrl}", _relativeFetchUrl);
        await using var htmlStream = await _httpClient.GetStreamAsync(_relativeFetchUrl, cancellationToken);
        doc.Load(htmlStream);

        var pdfNode = doc.DocumentNode.SelectSingleNode(_pdfLinkXPath);
        if (pdfNode == null) {
            return null;
        }
        var pdfLinkStr = _httpClient.BaseAddress + pdfNode.Attributes["href"].Value;
        if (!Uri.TryCreate(pdfLinkStr, UriKind.Absolute, out var pdfLink)) {
            throw new AviaServiceException($"Failed to parse fetch url '{pdfLinkStr}' from configuration");
        }
        _logger.LogInformation("Found PDF download link {PdfDownloadUrl}", pdfLink);
        return pdfLink;
    }
}