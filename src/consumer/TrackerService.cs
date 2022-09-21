using Common;
using Common.Models;
using Common.Repositories;
using Confluent.Kafka;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;

namespace Consumer
{
    internal class TrackerService : IHostedService
    {
        private readonly InfluxDBRepository _influxDb;
        private readonly InfluxDbConfig _influxDbConfig;
        private readonly KafkaConfig _kafkaConfig;

        public TrackerService(InfluxDBRepository influxDb,
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
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var conf = new ConsumerConfig
            {
                GroupId = _kafkaConfig.GroupId,
                BootstrapServers = _kafkaConfig.BootstrapServers,
                // Note: The AutoOffsetReset property determines the start offset in the event
                // there are not yet any committed offsets for the consumer group for the
                // topic/partitions of interest. By default, offsets are committed
                // automatically, so in this example, consumption will only start from the
                // earliest message in the topic 'my-topic' the first time you run the program.
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            using (var consumer = new ConsumerBuilder<Ignore, string>(conf).Build())
            {
                consumer.Subscribe(_kafkaConfig.TopicName);

                try
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            var cr = consumer.Consume(cancellationToken);

                            Log.Information($"Consumed message '{cr.Value}' at: '{cr.TopicPartitionOffset}'.");

                            var gpsRecord = JsonConvert.DeserializeObject<GpsRecord>(cr.Value);

                            if (gpsRecord != null)
                            {
                                _influxDb.Write(write =>
                                {
                                    var point = PointData.Measurement("gps-record")
                                        .Tag("registration", gpsRecord.Registration)
                                        .Field("altitude", gpsRecord.Position.Altitude)
                                        .Field("longitude", gpsRecord.Position.Longitude)
                                        .Field("latitude", gpsRecord.Position.Latitude)
                                        .Field("speed", gpsRecord.Speed)
                                        .Field("speed-unit", gpsRecord.SpeedUnit)
                                        .Timestamp(DateTime.UtcNow, WritePrecision.Ns);

                                    write.WritePoint(point, _kafkaConfig.BucketName, _kafkaConfig.Organization);
                                }, url: _influxDbConfig.Url, token: _influxDbConfig.Token);
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
                    // Ensure the consumer leaves the group cleanly and final offsets are committed.
                    consumer.Close();
                }
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("Influx Db consumer stopped.");

            return Task.CompletedTask;
        }
    }
}
