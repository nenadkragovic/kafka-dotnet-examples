using Common.Models;
using Consumer.Batch;
using Serilog;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostContext, configApp) =>
    {
        configApp.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((hostContext, services) =>
    {
        var config = hostContext.Configuration;

        services.Configure<InfluxDbConfig>(config.GetSection("InfluxDbConfig"));
        services.Configure<KafkaConfig>(config.GetSection("KafkaConfig"));
        services.AddHostedService<Worker>();

        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
    })
    .Build();

await host.RunAsync();
