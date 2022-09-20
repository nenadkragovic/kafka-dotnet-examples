using Common;
using Common.Models;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;

namespace Producer
{
    public class GpsTracker : IHostedService, IDisposable
    {
        private Timer? _timer = null;
        private Action<DeliveryReport<Null, string>> _handler;
        private IProducer<Null, string> _producer;
        private double _latitude, _longitude;
        private Random _random = new Random();
        private readonly KafkaConfig _kafkaConfig;

        public GpsTracker(IOptions<KafkaConfig> kafkaConfig)
        {
            _kafkaConfig = kafkaConfig.Value;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _latitude = _random.Next(-90, 90);
            _longitude = _random.Next(-180, 180);

            _handler = r =>
                Log.Information(
                    !r.Error.IsError
                        ? $"Delivered message to {r.TopicPartitionOffset}"
                        : $"Delivery Error: {r.Error.Reason}");

            _producer = new ProducerBuilder<Null, string>(
                new ProducerConfig { BootstrapServers = _kafkaConfig.BootstrapServers }).Build();

            _timer = new Timer(
                RecordPosition,
                null, TimeSpan.Zero,
                TimeSpan.FromSeconds(1));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            // wait for up to 10 seconds for any inflight messages to be delivered.
            _producer.Flush(TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private void RecordPosition(object? state)
        {
            _latitude += (_random.NextDouble() * 2 - 1)/1000;
            _longitude += (_random.NextDouble() * 2 - 1)/1000;

            _producer.Produce(_kafkaConfig.TopicName, new Message<Null, string>
            {
                Value = JsonConvert.SerializeObject(new GpsRecord()
                {
                    Registration = "YU-ABC",
                    Position = new Position()
                    {
                        Latitude = _latitude,
                        Longitude = _longitude,
                        Altitude = 37000,
                    },
                    Timestamp = DateTime.UtcNow,
                    Speed = "800",
                    SpeedUnit = "MPH"
                })
            }, _handler);
        }
    }
}
