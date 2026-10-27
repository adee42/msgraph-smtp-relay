namespace SMTPRelay.Models
{
    public class SmtpServerSettings
    {
        public int Port { get; set; } = 25;
        public int MaxMessageSize { get; set; } = 10485760;
    }
}
