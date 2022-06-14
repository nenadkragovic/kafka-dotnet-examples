using Producer;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }
    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddHostedService<GpsTracker>();

                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
            });
}