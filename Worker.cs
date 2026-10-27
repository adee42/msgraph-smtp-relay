using SmtpServer;
using SMTPRelay.Models;
using SMTPRelay.Services;
using SmtpServer.ComponentModel;
using SmtpServer.Storage;
using System.Net;

namespace SMTPRelay
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly SmtpServerSettings _smtpSettings;
        private readonly IServiceProvider _serviceProvider;
        private readonly EventLogService _eventLogService;
        private readonly SessionEventHandler _sessionEventHandler;
        private SmtpServer.SmtpServer? _smtpServer;

        public Worker(ILogger<Worker> logger, SmtpServerSettings smtpSettings, IServiceProvider serviceProvider, 
            EventLogService eventLogService, SessionEventHandler sessionEventHandler)
        {
            _logger = logger;
            _smtpSettings = smtpSettings;
            _serviceProvider = serviceProvider;
            _eventLogService = eventLogService;
            _sessionEventHandler = sessionEventHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SMTP Relay Service starting on port {Port}", _smtpSettings.Port);

            // Verify MessageStore and Factory can be resolved
            try
            {
                using var scope = _serviceProvider.CreateScope();

                var messageStore = scope.ServiceProvider.GetService<SmtpMessageStore>();
                if (messageStore != null)
                {
                    _logger.LogInformation("✓ SmtpMessageStore successfully resolved from service provider");
                }
                else
                {
                    _logger.LogError("✗ SmtpMessageStore could not be resolved from service provider!");
                }

                var messageStoreFactory = _serviceProvider.GetService<IMessageStoreFactory>();
                if (messageStoreFactory != null)
                {
                    _logger.LogInformation("✓ IMessageStoreFactory successfully resolved from service provider");
                }
                else
                {
                    _logger.LogError("✗ IMessageStoreFactory could not be resolved from service provider!");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving services from service provider");
            }

            // Configure SMTP server with explicit endpoint
            var options = new SmtpServerOptionsBuilder()
                .ServerName("SMTP Relay Service")
                .Endpoint(builder =>
                    builder
                        .Port(_smtpSettings.Port, isSecure: false)
                        .AllowUnsecureAuthentication())
                .MaxMessageSize(_smtpSettings.MaxMessageSize)
                .Build();

            _logger.LogInformation("SmtpServer options configured. Creating server instance...");
            _smtpServer = new SmtpServer.SmtpServer(options, _serviceProvider);

            // Attach session event handlers for diagnostic logging
            _smtpServer.SessionCreated += _sessionEventHandler.OnSessionCreated;
            _smtpServer.SessionCompleted += _sessionEventHandler.OnSessionCompleted;
            _smtpServer.SessionFaulted += _sessionEventHandler.OnSessionFaulted;

            _logger.LogInformation("SmtpServer instance created successfully with event handlers attached");

            try
            {
                _logger.LogInformation("Starting SMTP server...");
                await _smtpServer.StartAsync(stoppingToken);
                _logger.LogInformation("SMTP Relay Service started successfully on port {Port}", _smtpSettings.Port);

                // Log service startup to Windows Event Log
                _eventLogService.LogServiceStartup(_smtpSettings.Port);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("SMTP Relay Service is stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMTP Relay Service encountered an error");
                throw;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SMTP Relay Service is stopping");

            // Log service shutdown to Windows Event Log
            _eventLogService.LogServiceShutdown();

            await base.StopAsync(cancellationToken);
        }
    }
}
