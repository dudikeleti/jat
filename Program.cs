using System.Diagnostics;
var builder = WebApplication.CreateBuilder(args);
var app = WebApplication.Create();
app.MapGet("/ping", () => Sample.Work());
app.Run();
static class Sample
{
    public static string Work()
    {
        return "pong";
    }
}
