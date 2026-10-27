namespace SMTPRelay.Models
{
    public class GraphApiSettings
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string SendFromAddress { get; set; } = string.Empty;

        /// <summary>
        /// Additional mailboxes that may be used as the sender. Any other From address is replaced by SendFromAddress.
        /// </summary>
        public List<string> AllowedSenders { get; set; } = new();
    }
}
