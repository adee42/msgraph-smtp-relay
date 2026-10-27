using SmtpServer;
using SmtpServer.Storage;

namespace SMTPRelay.Services
{
    /// <summary>
    /// Factory for creating MessageStore instances from the service provider
    /// </summary>
    public class MessageStoreFactory : IMessageStoreFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MessageStoreFactory> _logger;

        public MessageStoreFactory(IServiceProvider serviceProvider, ILogger<MessageStoreFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IMessageStore CreateInstance(ISessionContext context)
        {
            _logger.LogInformation("MessageStoreFactory.CreateInstance called - creating SmtpMessageStore from service provider");

            // Create a scope to resolve transient services
            var scope = _serviceProvider.CreateScope();
            var messageStore = scope.ServiceProvider.GetRequiredService<SmtpMessageStore>();

            _logger.LogInformation("SmtpMessageStore instance created successfully via factory");

            return messageStore;
        }
    }
}
