using Common.Models;
using Common.Repositories;
using Consumer;
using Serilog;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostContext, configApp) =>
            {
                configApp.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                var config = hostContext.Configuration;

                services.Configure<InfluxDbConfig>(config.GetSection("InfluxDbConfig"));
                services.Configure<KafkaConfig>(config.GetSection("KafkaConfig"));
                services.AddSingleton<InfluxDBRepository>();
                services.AddHostedService<TrackerService>();

                Log.Logger = new LoggerConfiguration()
                    .WriteTo.Console()
                    .CreateLogger();
            });
}