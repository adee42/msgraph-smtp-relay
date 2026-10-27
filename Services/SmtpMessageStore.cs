using MimeKit;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using System.Buffers;

namespace SMTPRelay.Services
{
    public class SmtpMessageStore : MessageStore
    {
        private readonly GraphEmailService _graphEmailService;
        private readonly ILogger<SmtpMessageStore> _logger;
        private readonly EventLogService _eventLogService;

        public SmtpMessageStore(GraphEmailService graphEmailService, ILogger<SmtpMessageStore> logger, EventLogService eventLogService)
        {
            _graphEmailService = graphEmailService;
            _logger = logger;
            _eventLogService = eventLogService;

            // Log that the message store was created
            _logger.LogInformation("SmtpMessageStore initialized and ready to process messages");
        }

        public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, 
            ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
        {
            // Log that a message is being received
            _logger.LogInformation("SaveAsync called - processing incoming email from {RemoteEndpoint}", context.Properties.TryGetValue("RemoteEndPoint", out var endpoint) ? endpoint : "unknown");

            try
            {
                using var stream = new MemoryStream();
                foreach (var segment in buffer)
                {
                    await stream.WriteAsync(segment, cancellationToken);
                }
                stream.Position = 0;

                var message = await MimeMessage.LoadAsync(stream, cancellationToken);

                var toRecipients = message.To.Mailboxes.Select(m => m.Address).ToList();
                var ccRecipients = message.Cc.Mailboxes.Select(m => m.Address).ToList();
                var bccRecipients = message.Bcc.Mailboxes.Select(m => m.Address).ToList();

                var fromAddress = message.From.Mailboxes.FirstOrDefault()?.Address;
                var subject = message.Subject;

                // Preserve original format - use HTML if available, otherwise text
                var bodyContent = message.HtmlBody ?? message.TextBody ?? string.Empty;
                var isHtml = !string.IsNullOrEmpty(message.HtmlBody);

                // Extract attachments
                var attachments = await ExtractAttachmentsAsync(message, cancellationToken);

                // Calculate message size
                var messageSize = buffer.Length;

                // Log to standard logger
                _logger.LogInformation("Received email from {From} to {To} with subject: {Subject}, format: {Format}, attachments: {AttachmentCount}", 
                    fromAddress, string.Join(", ", toRecipients), subject, isHtml ? "HTML" : "Text", attachments?.Count ?? 0);

                // Log incoming email to Windows Event Log
                _eventLogService.LogIncomingEmail(
                    fromAddress ?? "(Unknown)",
                    string.Join(", ", toRecipients),
                    subject ?? "(No Subject)",
                    messageSize,
                    attachments?.Count ?? 0);

                // Attempt to send email via Graph API
                try
                {
                    await _graphEmailService.SendEmailAsync(
                        subject ?? "(No Subject)",
                        bodyContent,
                        isHtml,
                        toRecipients,
                        ccRecipients.Any() ? ccRecipients : null,
                        bccRecipients.Any() ? bccRecipients : null,
                        attachments,
                        fromAddress,
                        cancellationToken);

                    // Log successful forwarding to Windows Event Log
                    _eventLogService.LogOutgoingEmailSuccess(
                        fromAddress ?? "(Unknown)",
                        string.Join(", ", toRecipients),
                        subject ?? "(No Subject)");

                    return SmtpResponse.Ok;
                }
                catch (Exception graphEx)
                {
                    // Log failed forwarding to Windows Event Log
                    _eventLogService.LogOutgoingEmailFailure(
                        fromAddress ?? "(Unknown)",
                        string.Join(", ", toRecipients),
                        subject ?? "(No Subject)",
                        graphEx.Message);

                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process and forward email");
                return SmtpResponse.TransactionFailed;
            }
        }

        private async Task<List<Microsoft.Graph.Models.FileAttachment>?> ExtractAttachmentsAsync(MimeMessage message, CancellationToken cancellationToken)
        {
            var attachments = new List<Microsoft.Graph.Models.FileAttachment>();

            foreach (var attachment in message.Attachments)
            {
                if (attachment is MimePart mimePart)
                {
                    using var memoryStream = new MemoryStream();
                    await mimePart.Content.DecodeToAsync(memoryStream, cancellationToken);
                    var contentBytes = memoryStream.ToArray();

                    var fileAttachment = new Microsoft.Graph.Models.FileAttachment
                    {
                        OdataType = "#microsoft.graph.fileAttachment",
                        Name = mimePart.FileName ?? "attachment",
                        ContentType = mimePart.ContentType.MimeType,
                        ContentBytes = contentBytes
                    };

                    attachments.Add(fileAttachment);
                    _logger.LogInformation("Extracted attachment: {FileName} ({Size} bytes)", fileAttachment.Name, contentBytes.Length);
                }
            }

            return attachments.Any() ? attachments : null;
        }
    }
}
