using System.ComponentModel.DataAnnotations;

namespace Common
{
    public class Position
    {
        [Range(-90, 90)]
        public double Latitude { get; set; }
        [Range(-180, 80)]
        public double Longitude { get; set; }

        public Position(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }

    public class GpsRecord
    {
        public DateTime Timestamp { get; set; }
        public Position Position { get; set; } = null!;
    }
}