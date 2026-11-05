using StableRail.Api;
using StableRail.Infrastructure.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddStableRail("stablerail-api");
var app = builder.Build();
app.UseStableRail();
app.MapControlPlane();
app.Run();

public partial class Program;
