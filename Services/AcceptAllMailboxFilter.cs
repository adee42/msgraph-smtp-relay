using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;

namespace SMTPRelay.Services
{
    /// <summary>
    /// Mailbox filter that accepts all email addresses without validation
    /// </summary>
    public class AcceptAllMailboxFilter : IMailboxFilter, IMailboxFilterFactory
    {
        private readonly ILogger<AcceptAllMailboxFilter> _logger;

        public AcceptAllMailboxFilter(ILogger<AcceptAllMailboxFilter> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Accepts all sender addresses
        /// </summary>
        public Task<MailboxFilterResult> CanAcceptFromAsync(ISessionContext context, IMailbox from, int size, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Accepting sender: {From}", from.AsAddress());
            return Task.FromResult(MailboxFilterResult.Yes);
        }

        /// <summary>
        /// Accepts all recipient addresses
        /// </summary>
        public Task<MailboxFilterResult> CanDeliverToAsync(ISessionContext context, IMailbox to, IMailbox from, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Accepting recipient: {To} from {From}", to.AsAddress(), from.AsAddress());
            return Task.FromResult(MailboxFilterResult.Yes);
        }

        /// <summary>
        /// Creates an instance of this filter
        /// </summary>
        public IMailboxFilter CreateInstance(ISessionContext context)
        {
            return this;
        }
    }
}
