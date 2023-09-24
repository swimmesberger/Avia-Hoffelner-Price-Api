using System.Text.Json.Serialization;

namespace dev.wimmesberger.avia.price.tracker.Avia;

[JsonSerializable(typeof(AviaData))]
internal partial class AviaJsonSerializerContext : JsonSerializerContext { }
