using System.Collections;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace dev.wimmesberger.avia.price.tracker.Avia;

public sealed class AviaXsltContext : XsltContext {
    public override bool Whitespace => true;

    public override int CompareDocument(string baseUri, string nextbaseUri) => 0;

    public override bool PreserveWhitespace(XPathNavigator node) => true;

    public override IXsltContextFunction ResolveFunction(string prefix, string name, XPathResultType[] ArgTypes) {
        return name switch {
            "matches" => new MatchesXsltContextFunction(),
            _ => throw new NotImplementedException()
        };
    }

    public override IXsltContextVariable ResolveVariable(string prefix, string name) => null!;
}

internal sealed class MatchesXsltContextFunction : IXsltContextFunction {

    public XPathResultType[] ArgTypes => new XPathResultType[] { XPathResultType.String, XPathResultType.String };

    public int Maxargs => 2;

    public int Minargs => 2;

    public XPathResultType ReturnType => XPathResultType.Boolean;

    private Dictionary<string, Regex> _cachedRegex = new Dictionary<string, Regex>();

    public object Invoke(XsltContext xsltContext, object[] args, XPathNavigator docContext) {
        if (args.Length < Minargs) throw new Exception();
        string? itemValue;
        if (args[0] is XPathNodeIterator element) {
            itemValue = element.Current?.Value;
        } else {
            itemValue = args[0].ToString();
        }
        var itemPattern = args[1].ToString();
        if (string.IsNullOrWhiteSpace(itemValue) || string.IsNullOrWhiteSpace(itemPattern)) return false;
        var regex = _cachedRegex.GetOrAdd(itemPattern, x => new Regex(x));
        return regex.IsMatch(itemValue);
    }
}
