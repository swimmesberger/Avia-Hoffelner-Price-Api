using dev.wimmesberger.avia.price.tracker.Avia;

var builder = WebApplication.CreateSlimBuilder(args);
builder.AddAviaServices();

var app = builder.Build();
app.UseAviaServices();
app.Run();