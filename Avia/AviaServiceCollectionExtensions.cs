namespace dev.wimmesberger.avia.price.tracker.Avia;

public static class AviaServiceCollectionExtensions {
    public static IHostApplicationBuilder AddAviaServices(this IHostApplicationBuilder builder) {
        builder.Services.Configure<AviaConfiguration>(builder.Configuration.GetSection("Avia"));
        builder.Services.AddHttpClient<IAviaService, AviaService>();
        builder.Services.ConfigureHttpJsonOptions(options => {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, AviaJsonSerializerContext.Default);
        });
        return builder;
    }

    public static IEndpointRouteBuilder UseAviaServices(this IEndpointRouteBuilder app) {
        var aviaApi = app.MapGroup("/avia");
        aviaApi.MapGet("/", async (IAviaService aviaService, CancellationToken cancellationToken)
            => await aviaService.GetAviaData(cancellationToken));
        return app;
    }
}
