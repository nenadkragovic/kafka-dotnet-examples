using Common;
using Confluent.Kafka;
using Serilog;

namespace Producer
{
    public class GpsTracker : IHostedService, IDisposable
    {
        private Timer? _timer = null;
        private Action<DeliveryReport<Null, Position>> _handler;
        private IProducer<Null, Position> _producer;

        private ProducerConfig _conf = new ProducerConfig { BootstrapServers = "localhost:9092" };

        private double _latitude, _longitude;
        private Random _random = new Random();

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

            _producer = new ProducerBuilder<Null, Position>(_conf).Build();

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
            _latitude = _latitude + _random.NextDouble() * 2 - 1;
            _longitude = _longitude + _random.NextDouble() * 2 - 1;

            _producer.Produce("my-topic", new Message<Null, Position>
            {
                Value = new Position(_latitude, _longitude)
            }, _handler);
        }
    }
}
