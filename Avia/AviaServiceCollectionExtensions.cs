using dev.wimmesberger.avia.price.tracker.Avia.Contract;

namespace dev.wimmesberger.avia.price.tracker.Avia;

public static class AviaServiceCollectionExtensions {
    public static IHostApplicationBuilder AddAviaServices(this IHostApplicationBuilder builder) {
        builder.Services.Configure<AviaConfiguration>(builder.Configuration.GetSection("Avia"));
        builder.Services.Configure<AviaParsingConfiguration>(builder.Configuration.GetSection("Avia:Parsing"));
        builder.Services.AddHttpClient<IAviaPdfProvider, AviaPdfProvider>();
        builder.Services.AddTransient<IAviaPdfParser, AviaPdfParser>();
        builder.Services.AddTransient<IAviaService, AviaService>();
        builder.Services.ConfigureHttpJsonOptions(options => {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AviaJsonSerializerContext.Default);
        });
        return builder;
    }

    public static IEndpointRouteBuilder UseAviaServices(this IEndpointRouteBuilder app) {
        var aviaApi = app.MapGroup("/avia");
        aviaApi.MapGet("/entries", async (IAviaService aviaService, CancellationToken cancellationToken)
            => await aviaService.GetAviaData(cancellationToken));
        aviaApi.MapGet("/entries/today", async (IAviaService aviaService, CancellationToken cancellationToken)
            => await aviaService.GetCurrentAviaData(cancellationToken));
        return app;
    }
}
