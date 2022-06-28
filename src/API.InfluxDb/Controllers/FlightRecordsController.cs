using Common;
using Common.Repositories;
using Microsoft.AspNetCore.Mvc;
using Serilog;

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
        [Produces(typeof(IEnumerable<GpsRecord>))]
        public async Task<IEnumerable<GpsRecord>> Get()
        {
            var flux = "from(bucket:\"gps-route\") |> range(start: 0)";

            var tables = await _influxDb.GetQueryApi().QueryAsync(flux, "air-serbia");
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
                        Speed = (float)Double.Parse(record.Values["speed"].ToString()),
                        SpeedUnit = record.Values["speed-unit"].ToString()
                    }));

            return records;
        }
    }
}