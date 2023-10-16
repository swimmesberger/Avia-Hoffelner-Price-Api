using dev.wimmesberger.avia.price.tracker.Avia.Contract;
using Microsoft.Extensions.Options;
using System.Globalization;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace dev.wimmesberger.avia.price.tracker.Avia;

public sealed class AviaPdfParser : IAviaPdfParser {
    private readonly ILogger _logger;
    private readonly AviaParsingConfiguration _parsingConfiguration;
    private readonly NumberFormatInfo _priceFormat;

    // ReSharper disable once ConvertToPrimaryConstructor
    public AviaPdfParser(ILogger<AviaPdfParser> logger, IOptions<AviaParsingConfiguration> parsingConfiguration) {
        _logger = logger;
        _parsingConfiguration = parsingConfiguration.Value;
        // do not use culture and instead set fixed format to make the application InvariantGlobalization (AOT)
        _priceFormat = new NumberFormatInfo {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = "."
        };
    }

    public AviaData ParsePdf(Stream pdfStream) {
        _logger.LogInformation("Opening PDF");
        using PdfDocument pdfDocument = OpenPdf(pdfStream);
        _logger.LogInformation("Detecting tables in PDF");
        var aviaPriceTable = FindTable(pdfDocument) ?? throw new AviaServiceException("Did not detect price table in PDF");
        _logger.LogInformation("Parsing prices from table in PDF");
        return ParseTable(aviaPriceTable);
    }

    private PdfDocument OpenPdf(Stream pdfStream) {
        PdfDocument pdfDocument;
        // check if we can access the memory stream buffer and use that if possible (faster)
        if (pdfStream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var buffer) && buffer.Offset == 0) {
            pdfDocument = PdfDocument.Open(buffer.Array);
        } else {
            pdfDocument = PdfDocument.Open(pdfStream);
        }
        return pdfDocument;
    }

    private RawTable? FindTable(PdfDocument pdfDocument) {
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
        if (rows.Count == 0) {
            return new RawTable(postProcessedRows, 0);
        }
        return new RawTable(postProcessedRows, rows[0].Count);
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

    private sealed record RawTable(IReadOnlyList<IReadOnlyList<string>> Rows, int ColumnCount);
}