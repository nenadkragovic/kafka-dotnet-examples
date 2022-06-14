using System.ComponentModel.DataAnnotations;

namespace Common
{
    public class Position
    {
        [Range(-90, 90)]
        public double Latitude { get; set; }
        [Range(-180, 80)]
        public double Longitude { get; set; }

        public int Altitude { get; set; }
    }

    public class GpsRecord
    {
        public string Registration { get; set; }
        public DateTime Timestamp { get; set; }
        public Position? Position { get; set; }
        public float Speed { get; set; }
        public string SpeedUnit { get; set; } = "KMH";
    }
}