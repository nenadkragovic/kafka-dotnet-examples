using Common;
using Common.Repositories;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace API.InfluxDb.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FlightRecordsController : ControllerBase
    {

        private readonly InfluxDBRepository _influxDb;

        public FlightRecordsController(InfluxDBRepository influxDb)
        {
            _influxDb = influxDb;
        }

        [HttpGet(Name = "all")]
        [Produces(typeof(IDictionary<GpsRecord>))]
        public async Task<IDictionary<DateTime, GpsRecord>> Get([FromQuery] int offset, [FromQuery] int limit)
        {
            IDictionary<DateTime, GpsRecord> records = new Dictionary<DateTime, GpsRecord>();

            var flux = $"from(bucket:\"gps-routes1\") |> range(start: {offset})";

            var tables = await _influxDb.GetQueryApi().QueryAsync(flux, "air-serbia");

            foreach (var table in tables)
            {
                foreach (var record in table.Records)
                {
                    var registration = record.Values["registration"].ToString();
                    var timestamp = record.GetTimeInDateTime() ?? DateTime.MinValue;
                    if (!records.ContainsKey(record.GetTimeInDateTime() ?? DateTime.MinValue))
                        records[timestamp] = new GpsRecord()
                        {
                            Registration = registration,
                            Timestamp = timestamp,
                            Position = new Position()
                        };

                    FillPropertyData(records[timestamp], record.GetField(), record.GetValue());
                }
            }

            return records;
        }

        private void FillPropertyData(GpsRecord record, string field, object value)
        {
            switch (field)
            {
                case "altitude": record.Position.Altitude = (long)value; break;
                case "longitude": record.Position.Longitude = (double)value; break;
                case "latitude": record.Position.Latitude = (double)value; break;
                case "speed": record.Speed = (string)value.ToString(); break;
                case "speed-unit": record.SpeedUnit = (string)value; break;
            }
        }
    }
}