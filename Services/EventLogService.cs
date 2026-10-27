using System.Diagnostics;

namespace SMTPRelay.Services
{
    /// <summary>
    /// Service for logging events to the Windows Application Event Log
    /// </summary>
    public class EventLogService
    {
        private const string EventLogSource = "SMTP Relay Service";
        private const string EventLogName = "Application";
        private readonly ILogger<EventLogService> _logger;

        public EventLogService(ILogger<EventLogService> logger)
        {
            _logger = logger;
            EnsureEventSourceExists();
        }

        /// <summary>
        /// Ensures the event source is created for the application
        /// </summary>
        private void EnsureEventSourceExists()
        {
            try
            {
                if (!EventLog.SourceExists(EventLogSource))
                {
                    EventLog.CreateEventSource(EventLogSource, EventLogName);
                    _logger.LogInformation("Event source '{EventLogSource}' created successfully", EventLogSource);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create event source. The service may need to run with elevated permissions once to create the event source.");
            }
        }

        /// <summary>
        /// Logs information about an incoming email
        /// </summary>
        public void LogIncomingEmail(string from, string to, string subject, long sizeInBytes, int attachmentCount)
        {
            try
            {
                var message = $"Incoming Email Received:\n" +
                             $"From: {from}\n" +
                             $"To: {to}\n" +
                             $"Subject: {subject}\n" +
                             $"Size: {FormatBytes(sizeInBytes)}\n" +
                             $"Attachments: {attachmentCount}";

                WriteEventLog(message, EventLogEntryType.Information, 1001);
                _logger.LogInformation("Logged incoming email to Event Log: From={From}, To={To}, Subject={Subject}", from, to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write incoming email to Event Log");
            }
        }

        /// <summary>
        /// Logs the successful forwarding of an email
        /// </summary>
        public void LogOutgoingEmailSuccess(string from, string to, string subject)
        {
            try
            {
                var message = $"Email Forwarded Successfully:\n" +
                             $"From: {from}\n" +
                             $"To: {to}\n" +
                             $"Subject: {subject}\n" +
                             $"Status: Sent via Microsoft Graph API";

                WriteEventLog(message, EventLogEntryType.Information, 1002);
                _logger.LogInformation("Logged successful email forwarding to Event Log: To={To}, Subject={Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write outgoing email success to Event Log");
            }
        }

        /// <summary>
        /// Logs a failed email forwarding attempt
        /// </summary>
        public void LogOutgoingEmailFailure(string from, string to, string subject, string errorMessage)
        {
            try
            {
                var message = $"Email Forwarding Failed:\n" +
                             $"From: {from}\n" +
                             $"To: {to}\n" +
                             $"Subject: {subject}\n" +
                             $"Status: Failed\n" +
                             $"Error: {errorMessage}";

                WriteEventLog(message, EventLogEntryType.Error, 1003);
                _logger.LogWarning("Logged failed email forwarding to Event Log: To={To}, Subject={Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write outgoing email failure to Event Log");
            }
        }

        /// <summary>
        /// Logs service startup
        /// </summary>
        public void LogServiceStartup(int port)
        {
            try
            {
                var message = $"SMTP Relay Service Started:\n" +
                             $"Listening on port: {port}\n" +
                             $"Ready to accept SMTP connections";

                WriteEventLog(message, EventLogEntryType.Information, 1000);
                _logger.LogInformation("Logged service startup to Event Log");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write service startup to Event Log");
            }
        }

        /// <summary>
        /// Logs service shutdown
        /// </summary>
        public void LogServiceShutdown()
        {
            try
            {
                var message = "SMTP Relay Service Stopped";

                WriteEventLog(message, EventLogEntryType.Information, 1004);
                _logger.LogInformation("Logged service shutdown to Event Log");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write service shutdown to Event Log");
            }
        }

        /// <summary>
        /// Writes an entry to the Windows Event Log
        /// </summary>
        private void WriteEventLog(string message, EventLogEntryType entryType, int eventId)
        {
            try
            {
                using var eventLog = new EventLog(EventLogName);
                eventLog.Source = EventLogSource;
                eventLog.WriteEntry(message, entryType, eventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to Event Log. EventID: {EventId}, Message: {Message}", eventId, message);
            }
        }

        /// <summary>
        /// Formats byte size into human-readable format
        /// </summary>
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
}
