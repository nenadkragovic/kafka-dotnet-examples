using Common.Models;
using Common.Repositories;
using Confluent.Kafka;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Concurrent;

namespace Consumer.Batch
{
    public class Worker : BackgroundService
    {
        private readonly InfluxDBRepository _influxDb;
        private readonly InfluxDbConfig _influxDbConfig;
        private readonly KafkaConfig _kafkaConfig;

        private readonly ConcurrentBag<GpsRecord> _records = new ConcurrentBag<GpsRecord>();
        private static object _locker = new object();
        private volatile Timer _timer;

        public Worker(InfluxDBRepository influxDb,
                      IOptions<InfluxDbConfig> influxDbConfig,
                      IOptions<KafkaConfig> kafkaConfig)
        {
            _influxDbConfig = influxDbConfig.Value;
            _kafkaConfig = kafkaConfig.Value;

            _influxDb = influxDb;
            _influxDb.CreateOrganizationAndBucket(_kafkaConfig.Organization,
                                                  _kafkaConfig.BucketName,
                                                  _influxDbConfig.Url,
                                                  _influxDbConfig.Token);

            _timer = new Timer(WriteDataToDb,
                               null,
                               TimeSpan.Zero,
                               TimeSpan.FromMilliseconds(5000));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var conf = new ConsumerConfig
            {
                GroupId = _kafkaConfig.GroupId,
                BootstrapServers = _kafkaConfig.BootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            using (var consumer = new ConsumerBuilder<Ignore, string>(conf).Build())
            {
                consumer.Subscribe(_kafkaConfig.TopicName);

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var cr = consumer.Consume(stoppingToken);

                            Log.Information($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");

                            var gpsRecord = JsonConvert.DeserializeObject<GpsRecord>(cr.Value);

                            if (gpsRecord != null)
                            {
                                lock (_locker)
                                {
                                    _records.Add(gpsRecord);
                                }
                            }
                            else
                            {
                                Log.Warning("Couldn't parse data");
                            }

                        }
                        catch (ConsumeException e)
                        {
                            Log.Error($"Error occured: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    consumer.Close();
                }
            }
        }

        private void WriteDataToDb(object parameters)
        {
            lock (_locker)
            {
                if (!_records.Any())
                    return;

                _influxDb.Write(write =>
                {
                    var points = new List<PointData>();

                    foreach (var gpsRecord in _records)
                    {
                        points.Add(PointData.Measurement("gps-record")
                            .Tag("registration", gpsRecord.Registration)
                            .Field("altitude", gpsRecord.Position.Altitude)
                            .Field("longitude", gpsRecord.Position.Longitude)
                            .Field("latitude", gpsRecord.Position.Latitude)
                            .Field("speed", gpsRecord.Speed)
                            .Field("speed-unit", gpsRecord.SpeedUnit)
                            .Timestamp(DateTime.UtcNow, WritePrecision.Ns));
                    }

                    write.WritePoints(points, _kafkaConfig.BucketName, _kafkaConfig.Organization);

                    points.Clear();

                }, url: _influxDbConfig.Url, token: _influxDbConfig.Token);
            }
        }
    }
}