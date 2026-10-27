using SmtpServer;
using SmtpServer.Net;

namespace SMTPRelay.Services
{
    /// <summary>
    /// Session event handler to track SMTP session lifecycle and diagnose issues
    /// </summary>
    public class SessionEventHandler
    {
        private readonly ILogger<SessionEventHandler> _logger;

        public SessionEventHandler(ILogger<SessionEventHandler> logger)
        {
            _logger = logger;
        }

        public void OnSessionCreated(object? sender, SessionEventArgs e)
        {
            _logger.LogInformation("SMTP Session Created - Remote: {RemoteEndpoint}, SessionId: {SessionId}", 
                e.Context.Properties.TryGetValue(EndpointListener.RemoteEndPointKey, out var endpoint) ? endpoint : "unknown",
                e.Context.Properties.TryGetValue("SessionId", out var sessionId) ? sessionId : "unknown");
            
            e.Context.CommandExecuting += OnCommandExecuting;
            e.Context.CommandExecuted += OnCommandExecuted;
        }

        public void OnSessionCompleted(object? sender, SessionEventArgs e)
        {
            _logger.LogInformation("SMTP Session Completed - Remote: {RemoteEndpoint}", 
                e.Context.Properties.TryGetValue(EndpointListener.RemoteEndPointKey, out var endpoint) ? endpoint : "unknown");
        }

        public void OnSessionFaulted(object? sender, SessionFaultedEventArgs e)
        {
            _logger.LogError(e.Exception, "SMTP Session Faulted - Remote: {RemoteEndpoint}", 
                e.Context.Properties.TryGetValue(EndpointListener.RemoteEndPointKey, out var endpoint) ? endpoint : "unknown");
        }

        private void OnCommandExecuting(object? sender, SmtpCommandEventArgs e)
        {
            _logger.LogInformation("SMTP Command Executing: {Command}", e.Command.Name);
        }

        private void OnCommandExecuted(object? sender, SmtpCommandEventArgs e)
        {
            _logger.LogInformation("SMTP Command Executed: {Command}", e.Command.Name);
        }
    }
}
