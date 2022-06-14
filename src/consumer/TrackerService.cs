using System.Text.Json.Serialization;
using Common;
using Confluent.Kafka;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Newtonsoft.Json;
using Serilog;

namespace Consumer
{
    internal class TrackerService : IHostedService
    {
        private readonly InfluxDBService _influxDb;

        public TrackerService(InfluxDBService influxDb)
        {
            _influxDb = influxDb;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var conf = new ConsumerConfig
            {
                GroupId = "test-consumer-group",
                BootstrapServers = "localhost:9092",
                // Note: The AutoOffsetReset property determines the start offset in the event
                // there are not yet any committed offsets for the consumer group for the
                // topic/partitions of interest. By default, offsets are committed
                // automatically, so in this example, consumption will only start from the
                // earliest message in the topic 'my-topic' the first time you run the program.
                AutoOffsetReset = AutoOffsetReset.Earliest,
            };

            using (var consumer = new ConsumerBuilder<Ignore, string>(conf).Build())
            {
                consumer.Subscribe("my-topic");

                //CancellationTokenSource cts = new CancellationTokenSource();
                //Console.CancelKeyPress += (_, e) => {
                //    e.Cancel = true; // prevent the process from terminating.
                //    cts.Cancel();
                //};

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

                                    write.WritePoint(point, "gps-route", "air-serbia");
                                });
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

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var results = await _influxDb.QueryAsync(async query =>
            {
                var flux = "from(bucket:\"gps-route\") |> range(start: 0)";
                var tables = await query.QueryAsync(flux, "air-serbia");
                var records = tables.SelectMany(table =>
                    table.Records.Select(record =>
                        new GpsRecord()
                        {
                            Registration = record.Values["registration"].ToString(),
                            Position = new Position()
                            {
                                Latitude = Double.Parse(record.Values["latitude"].ToString()),
                                Longitude = Double.Parse(record.Values["longitude"].ToString()),
                                Altitude = int.Parse(record.Values["altitude"].ToString()),
                            },
                            Timestamp = DateTime.Parse(record.GetTime().ToString()),
                            Speed = 800,
                            SpeedUnit = "MPH"
                        }));

                return records;
            });
        }
    }
}
