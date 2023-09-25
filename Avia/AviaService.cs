using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Xml.XPath;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace dev.wimmesberger.avia.price.tracker.Avia;

public sealed class AviaService : IAviaService {
    private readonly ILogger _logger;
    private readonly Uri _relativeFetchUrl;
    private readonly HttpClient _httpClient;
    private readonly AviaParsingConfiguration _parsingConfiguration;
    private readonly NumberFormatInfo _priceFormat;
    private readonly XPathExpression _pdfLinkXPath;

    public AviaService(ILogger<AviaService> logger, IOptions<AviaConfiguration> configurationOptions, HttpClient httpClient) {
        _logger = logger;
        _httpClient = httpClient;
        var configuration = configurationOptions.Value;
        _parsingConfiguration = configuration.Parsing;
        if (!Uri.TryCreate(configuration.FetchUrl, UriKind.Absolute, out var fetchUrl)) {
            throw new AviaServiceException($"Failed to parse fetch url '{configuration.FetchUrl}' from configuration");
        }
        _httpClient.BaseAddress = new Uri(fetchUrl.GetLeftPart(UriPartial.Authority));
        _relativeFetchUrl = _httpClient.BaseAddress.MakeRelativeUri(fetchUrl);
        // do not use culture and instead set fixed format to make the application InvariantGlobalization (AOT)
        _priceFormat = new NumberFormatInfo {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = "."
        };
        _pdfLinkXPath = XPathExpression.Compile(configuration.PdfXPath);
        _pdfLinkXPath.SetContext(new AviaXsltContext());
    }

    public async ValueTask<AviaData> GetAviaData(CancellationToken cancellationToken = default) {
        _logger.LogInformation("Fetching avia downloads page {FetchUrl}", _relativeFetchUrl);
        var pdfDownloadUrl = await GetAviaPdfLink(cancellationToken) ?? throw new AviaServiceException("Did not find PDF download url");
        _logger.LogInformation("Found PDF download link {PdfDownloadUrl}", pdfDownloadUrl);
        _logger.LogInformation("Downloading PDF from {FetchUrl}", pdfDownloadUrl);
        var pdfData = await _httpClient.GetByteArrayAsync(pdfDownloadUrl, cancellationToken);
        _logger.LogInformation("Opening PDF");
        using var pdfDocument = PdfDocument.Open(pdfData);
        _logger.LogInformation("Detecting tables in PDF");
        var aviaPriceTable = GetAviaPriceTableV2(pdfDocument) ?? throw new AviaServiceException("Did not detect price table in PDF");
        _logger.LogInformation("Parsing prices from table in PDF");
        var aviaParsedPriceTable = ParseTable(aviaPriceTable);
        return aviaParsedPriceTable;
    }


    private async ValueTask<string?> GetAviaPdfLink(CancellationToken cancellationToken = default) {
        var doc = new HtmlDocument();
        await using var htmlStream = await _httpClient.GetStreamAsync(_relativeFetchUrl, cancellationToken);
        doc.Load(htmlStream);

        var pdfNode = doc.DocumentNode.SelectSingleNode(_pdfLinkXPath);
        if (pdfNode == null) {
            return null;
        }
        return pdfNode.Attributes["href"].Value;
    }

    private RawTable? GetAviaPriceTableV2(PdfDocument pdfDocument) {
        if (pdfDocument.NumberOfPages == 0) return null;
        var rows = new List<List<string>>(20);

        int rowIdx = 0;
        double? lastY = null;
        foreach (Word word in pdfDocument.GetPage(1).GetWords()) {
            var topLeftOfWordY = word.BoundingBox.TopLeft.Y;
            if (topLeftOfWordY > _parsingConfiguration.MaximumYCoord) continue;
            if (lastY != null && (lastY-topLeftOfWordY) > _parsingConfiguration.MinimumYCoordDelta) {
                rowIdx++;
            }
            lastY = topLeftOfWordY;

            var text = word.Text;
            List<string> row;
            if (rows.Count <= rowIdx) {
                row = new List<string>(20);
                rows.Add(row);
            } else {
                row = rows[rowIdx];
            }
            row.Add(text);
        }
        var postProcessedRows = rows
            .Select((x, idx) =>
                // remove Bruttopreis/Nettopreis from first column
                // remove ct/kWh from last column
                idx == 0 ? x.AsReadOnly() : x[1..^1].AsReadOnly())
            .ToList();
        return new RawTable(postProcessedRows, 15);
    }

    private AviaData ParseTable(RawTable table) {
        var entries = new List<AviaDataEntry>(20);
        for (int i = 0; i<table.ColumnCount; i++) {
            entries.Add(ParseColumn(table, i));
        }
        return new AviaData() { Entries = entries };
    }

    private AviaDataEntry ParseColumn(RawTable table, int columnIdx) {
        var dateText = table.Rows[0][columnIdx];
        var grossPriceText = table.Rows[1][columnIdx];
        var netPriceText = table.Rows[2][columnIdx];
        var dateOfMonth = ParseDate(dateText);
        if (!decimal.TryParse(grossPriceText, _priceFormat, out var grossPriceCtkWH)) {
            throw new AviaServiceException($"Failed to parse gross price '{grossPriceText}' from PDF table");
        }
        if (!decimal.TryParse(netPriceText, _priceFormat, out var netPriceCtkwH)) {
            throw new AviaServiceException($"Failed to parse net price '{netPriceText}' from PDF table");
        }
        return new AviaDataEntry(dateOfMonth, grossPriceCtkWH, netPriceCtkwH);
    }

    private DateOnly ParseDate(string date) {
        var parts = date.Split('.');
        var monthName = parts[0];
        if (!DateOnly.TryParseExact(parts[1], "yy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedYearDate)) {
            throw new AviaServiceException($"Failed to parse date '{date}' from PDF table");
        }
        // the pdf uses https://unicode-org.github.io/icu/userguide/format_parse/datetime/#date-field-symbol-table LLL formatting
        // LLL and MMM differs for some german months regarding 3 or 4 letters
        // LLL is not supported in .NET currently
        var monthNum = monthName switch {
            "Jän" => 1,
            "Feb" => 2,
            "Mär" => 3, // März in locale
            "Apr" => 4,
            "Mai" => 5,
            "Jun" => 6, // Juni in locale
            "Jul" => 7, // Juli in locale
            "Aug" => 8,
            "Sep" => 9,
            "Okt" => 10,
            "Nov" => 11,
            "Dez" => 12,
            _ => throw new AviaServiceException($"Failed to parse date '{date}' from PDF table")
        };
        return new DateOnly(parsedYearDate.Year, monthNum, 1);
    }

    internal sealed record RawTable(IReadOnlyList<IReadOnlyList<string>> Rows, int ColumnCount);
}
