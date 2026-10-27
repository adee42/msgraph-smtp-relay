using SmtpServer;
using SmtpServer.Authentication;

namespace SMTPRelay.Services
{
    /// <summary>
    /// User authenticator that allows all connections without authentication
    /// </summary>
    public class NoAuthenticationUserAuthenticator : IUserAuthenticator, IUserAuthenticatorFactory
    {
        private readonly ILogger<NoAuthenticationUserAuthenticator> _logger;

        public NoAuthenticationUserAuthenticator(ILogger<NoAuthenticationUserAuthenticator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Always returns true to allow unauthenticated connections
        /// </summary>
        public Task<bool> AuthenticateAsync(ISessionContext context, string user, string password, CancellationToken cancellationToken)
        {
            // Allow all connections without authentication
            _logger.LogInformation("Authentication requested for user: {User} - allowing without validation", user);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Creates an instance of this authenticator
        /// </summary>
        public IUserAuthenticator CreateInstance(ISessionContext context)
        {
            _logger.LogInformation("Creating NoAuthenticationUserAuthenticator instance");
            return this;
        }
    }
}
