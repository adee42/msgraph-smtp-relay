namespace SMTPRelay.Models
{
    public class SmtpServerSettings
    {
        public int Port { get; set; } = 25;
        public int MaxMessageSize { get; set; } = 10485760;

        /// <summary>
        /// IP addresses or CIDR ranges allowed to send mail through the relay. Empty allows every client.
        /// </summary>
        public List<string> AllowedClients { get; set; } = new();
    }
}
