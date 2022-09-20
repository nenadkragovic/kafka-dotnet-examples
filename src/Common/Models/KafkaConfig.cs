namespace Common.Models
{
    public class KafkaConfig
    {
        public string TopicName { get; set; }
        public string Organization { get; set; }
        public string BucketName { get; set; }
        public string GroupId { get; set; }
        public string BootstrapServers { get; set; }
    }
}
