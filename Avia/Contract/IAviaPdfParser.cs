namespace dev.wimmesberger.avia.price.tracker.Avia.Contract;

public interface IAviaPdfParser {
    AviaData ParsePdf(Stream pdfStream);
}