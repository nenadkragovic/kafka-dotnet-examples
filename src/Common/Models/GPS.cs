using System.ComponentModel.DataAnnotations;

namespace Common.Models
{
    public class Position
    {
        [Range(-90, 90)]
        public double Latitude { get; set; }
        [Range(-180, 80)]
        public double Longitude { get; set; }
        public long Altitude { get; set; }
    }

    public class GpsRecord
    {
        public string Registration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Position? Position { get; set; }
        public string Speed { get; set; }
        public string SpeedUnit { get; set; } = "KMH";
    }
}