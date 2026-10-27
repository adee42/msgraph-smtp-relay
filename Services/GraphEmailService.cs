using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using SMTPRelay.Models;

namespace SMTPRelay.Services
{
    /// <summary>
    /// Service for sending emails through Microsoft Graph API using application permissions
    /// </summary>
    public class GraphEmailService
    {
        // Microsoft Graph client for API operations
        private readonly GraphServiceClient _graphClient;
        // Configuration settings for Graph API authentication
        private readonly GraphApiSettings _settings;
        // Logger for tracking email operations and errors
        private readonly ILogger<GraphEmailService> _logger;

        /// <summary>
        /// Initializes the Graph Email Service with authentication credentials
        /// </summary>
        public GraphEmailService(GraphApiSettings settings, ILogger<GraphEmailService> logger)
        {
            _settings = settings;
            _logger = logger;

            // Create client secret credential for app-only authentication
            var clientSecretCredential = new ClientSecretCredential(
                settings.TenantId,
                settings.ClientId,
                settings.ClientSecret);

            // Initialize Graph client with the credential
            _graphClient = new GraphServiceClient(clientSecretCredential);
        }

        /// <summary>
        /// Sends an email via Microsoft Graph API with support for HTML/Text content, multiple recipients, and attachments
        /// </summary>
        public async Task SendEmailAsync(string subject, string bodyContent, bool isHtml, List<string> toRecipients, 
            List<string>? ccRecipients = null, List<string>? bccRecipients = null, 
            List<FileAttachment>? attachments = null, string? fromAddress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Create the email message with subject, body, and primary recipients
                var message = new Message
                {
                    Subject = subject,
                    Body = new ItemBody
                    {
                        ContentType = isHtml ? BodyType.Html : BodyType.Text,
                        Content = bodyContent
                    },
                    ToRecipients = toRecipients.Select(email => new Recipient
                    {
                        EmailAddress = new EmailAddress { Address = email }
                    }).ToList()
                };

                // Add CC recipients if provided
                if (ccRecipients?.Any() == true)
                {
                    message.CcRecipients = ccRecipients.Select(email => new Recipient
                    {
                        EmailAddress = new EmailAddress { Address = email }
                    }).ToList();
                }

                // Add BCC recipients if provided
                if (bccRecipients?.Any() == true)
                {
                    message.BccRecipients = bccRecipients.Select(email => new Recipient
                    {
                        EmailAddress = new EmailAddress { Address = email }
                    }).ToList();
                }

                // Add file attachments if provided
                if (attachments?.Any() == true)
                {
                    message.Attachments = attachments.Cast<Attachment>().ToList();
                }

                // Determine the sender address (use provided or default from settings)
                var sendFrom = fromAddress ?? _settings.SendFromAddress;

                // Prepare the send mail request
                var requestBody = new SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = true
                };

                // Send the email through Microsoft Graph API
                await _graphClient.Users[sendFrom].SendMail.PostAsync(requestBody, cancellationToken: cancellationToken);

                // Log successful send operation
                _logger.LogInformation("Email sent successfully via Graph API to {Recipients}", string.Join(", ", toRecipients));
            }
            // Handle and log any errors during email sending
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via Graph API" + ex.Message);
                throw;
            }
        }
    }
}
