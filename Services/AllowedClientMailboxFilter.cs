using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Net;
using SmtpServer.Storage;
using SMTPRelay.Models;
using System.Net;

namespace SMTPRelay.Services
{
    /// <summary>
    /// Mailbox filter that only accepts mail from clients listed in SmtpServer:AllowedClients
    /// </summary>
    public class AllowedClientMailboxFilter : IMailboxFilter, IMailboxFilterFactory
    {
        private readonly ILogger<AllowedClientMailboxFilter> _logger;
        private readonly List<IPNetwork> _allowedNetworks;

        public AllowedClientMailboxFilter(SmtpServerSettings settings, ILogger<AllowedClientMailboxFilter> logger)
        {
            _logger = logger;
            _allowedNetworks = settings.AllowedClients.Select(ParseNetwork).ToList();
        }

        /// <summary>
        /// True when every client is allowed because no allowlist is configured
        /// </summary>
        public bool AllowsAnyClient => _allowedNetworks.Count == 0;

        /// <summary>
        /// Accepts the sender only if the client IP is allowed
        /// </summary>
        public Task<bool> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
        {
            var remoteAddress = GetRemoteAddress(context);

            if (!IsAllowed(remoteAddress))
            {
                _logger.LogWarning("Rejecting sender {From}: client {RemoteAddress} is not in AllowedClients", from.AsAddress(), remoteAddress?.ToString() ?? "unknown");
                return Task.FromResult(false);
            }

            _logger.LogInformation("Accepting sender: {From} from client {RemoteAddress}", from.AsAddress(), remoteAddress);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Accepts all recipient addresses
        /// </summary>
        public Task<bool> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Accepting recipient: {To} from {From}", to.AsAddress(), from.AsAddress());
            return Task.FromResult(true);
        }

        /// <summary>
        /// Creates an instance of this filter
        /// </summary>
        public IMailboxFilter CreateInstance(ISessionContext context)
        {
            return this;
        }

        private bool IsAllowed(IPAddress? address)
        {
            if (AllowsAnyClient)
            {
                return true;
            }

            return address != null && _allowedNetworks.Any(network => network.Contains(address));
        }

        private static IPAddress? GetRemoteAddress(ISessionContext context)
        {
            if (!context.Properties.TryGetValue(EndpointListener.RemoteEndPointKey, out var value) || value is not IPEndPoint endpoint)
            {
                return null;
            }

            // Dual-mode sockets report IPv4 clients as IPv4-mapped IPv6 addresses
            return endpoint.Address.IsIPv4MappedToIPv6 ? endpoint.Address.MapToIPv4() : endpoint.Address;
        }

        private static IPNetwork ParseNetwork(string entry)
        {
            var value = entry.Trim();

            // Accept single addresses as well as CIDR ranges
            if (!value.Contains('/') && IPAddress.TryParse(value, out var address))
            {
                value = $"{address}/{(address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128)}";
            }

            if (!IPNetwork.TryParse(value, out var network))
            {
                throw new InvalidOperationException($"Invalid entry '{entry}' in SmtpServer:AllowedClients. Use an IP address or a CIDR range such as 192.168.1.0/24.");
            }

            return network;
        }
    }
}
