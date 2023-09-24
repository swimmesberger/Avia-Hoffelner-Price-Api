namespace dev.wimmesberger.avia.price.tracker.Avia;

public class AviaServiceException : Exception {
    public AviaServiceException() { }
    public AviaServiceException(string message) : base(message) { }
}
