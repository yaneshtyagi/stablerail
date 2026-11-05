using StableRail.Infrastructure.Hosting;
using StableRail.Infrastructure.Queue;

var builder = WebApplication.CreateBuilder(args);
builder.AddStableRail("stablerail-worker");
builder.Services.AddHostedService<QueuePresenceService>();
var app = builder.Build();
app.UseStableRail();
app.Run();

public partial class Program;
