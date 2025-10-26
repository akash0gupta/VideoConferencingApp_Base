using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using VideoConferencingApp.Domain.DTOs.Notification;
using VideoConferencingApp.Infrastructure.Configuration;
using VideoConferencingApp.Infrastructure.Configuration.Settings;
using static LinqToDB.Reflection.Methods.LinqToDB.Insert;
using System.Text;
using VideoConferencingApp.Application.Interfaces.Common.INotificationServices;

namespace VideoConferencingApp.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _emailSettings;

        public EmailService(ILogger<EmailService> logger, AppSettings appSettings)
        {
            _logger = logger;
            _emailSettings = appSettings.Get<EmailSettings>();
        }

        public async Task SendEmailAsync(EmailMessage emailMessage)
        {
            try
            {
                _logger.LogInformation("Sending email to {To} with subject {Subject}",
                    string.Join(", ", emailMessage.To), emailMessage.Subject);

                // Validate email message
                ValidateEmailMessage(emailMessage);

                // Create MimeMessage
                var message = CreateMimeMessage(emailMessage);

                // Send email
                await SendViaMailKit(message);

                _logger.LogInformation("Email sent successfully to {To}", string.Join(", ", emailMessage.To));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {To}", string.Join(", ", emailMessage.To));
                throw;
            }
        }

        // Backward compatibility method
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var emailMessage = new EmailMessage
            {
                To = new List<string> { to },
                Subject = subject,
                Body = body
            };

            await SendEmailAsync(emailMessage);
        }

        public async Task SendTemplatedEmailAsync<T>(EmailTemplateMessage<T> templateMessage)
        {
            try
            {
                _logger.LogInformation("Sending templated email {Template} to {To}",
                    templateMessage.TemplateName, string.Join(", ", templateMessage.To));

                // Load template and replace placeholders + table data
                var body = await LoadTemplate(templateMessage.TemplateName,
                    templateMessage.TemplateData, templateMessage.TableData);

                var emailMessage = new EmailMessage
                {
                    To = templateMessage.To,
                    Cc = templateMessage.Cc,
                    Bcc = templateMessage.Bcc,
                    ReplyTo = templateMessage.ReplyTo,
                    Subject = templateMessage.Subject,
                    Body = body,
                    IsHtml = templateMessage.IsHtml,
                    Attachments = templateMessage.Attachments,
                    Priority = templateMessage.Priority
                };

                await SendEmailAsync(emailMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending templated email to {To}",
                    string.Join(", ", templateMessage.To));
                throw;
            }
        }

        // Backward compatibility method
        public async Task SendTemplatedEmailAsync<T>(
            string to,
            string subject,
            string templateName,
            Dictionary<string, string> data,
            List<T> tableData = null)
        {
            var templateMessage = new EmailTemplateMessage<T>
            {
                To = new List<string> { to },
                Subject = subject,
                TemplateName = templateName,
                TemplateData = data,
                TableData = tableData
            };

            await SendTemplatedEmailAsync(templateMessage);
        }

        private MimeMessage CreateMimeMessage(EmailMessage emailMessage)
        {
            var message = new MimeMessage();

            // From
            var fromAddress = _emailSettings.FromAddress;
            var fromName = _emailSettings.FromName;
            message.From.Add(new MailboxAddress(fromName, fromAddress));

            // To
            foreach (var to in emailMessage.To)
            {
                message.To.Add(MailboxAddress.Parse(to));
            }

            // CC
            if (emailMessage.Cc?.Any() == true)
            {
                foreach (var cc in emailMessage.Cc)
                {
                    message.Cc.Add(MailboxAddress.Parse(cc));
                }
            }

            // BCC
            if (emailMessage.Bcc?.Any() == true)
            {
                foreach (var bcc in emailMessage.Bcc)
                {
                    message.Bcc.Add(MailboxAddress.Parse(bcc));
                }
            }

            // Reply-To
            if (emailMessage.ReplyTo?.Any() == true)
            {
                foreach (var replyTo in emailMessage.ReplyTo)
                {
                    message.ReplyTo.Add(MailboxAddress.Parse(replyTo));
                }
            }

            // Subject
            message.Subject = emailMessage.Subject;
            
            // Create body
            var bodyBuilder = new BodyBuilder();

            if (emailMessage.IsHtml)
            {
                bodyBuilder.HtmlBody = emailMessage.Body;

                // Optionally add plain text version
                if (!string.IsNullOrEmpty(emailMessage.PlainTextBody))
                {
                    bodyBuilder.TextBody = emailMessage.PlainTextBody;
                }
            }
            else
            {
                bodyBuilder.TextBody = emailMessage.Body;
            }

            // Add attachments
            if (emailMessage.Attachments?.Any() == true)
            {
                foreach (var attachment in emailMessage.Attachments)
                {
                    if (attachment.FileStream != null)
                    {
                        bodyBuilder.Attachments.Add(
                            attachment.FileName,
                            attachment.FileStream,
                            MimeKit.ContentType.Parse(attachment.ContentType ?? "application/octet-stream"));
                    }
                    else if (attachment.FileBytes != null)
                    {
                        bodyBuilder.Attachments.Add(
                            attachment.FileName,
                            attachment.FileBytes,
                            MimeKit.ContentType.Parse(attachment.ContentType ?? "application/octet-stream"));
                    }

                    // Set content ID for inline attachments (e.g., embedded images)
                    if (!string.IsNullOrEmpty(attachment.ContentId))
                    {
                        var mimeAttachment = bodyBuilder.Attachments.Last();
                        mimeAttachment.ContentId = attachment.ContentId;
                        mimeAttachment.ContentDisposition.Disposition = MimeKit.ContentDisposition.Inline;
                    }
                }
            }

            // Add linked resources (embedded images)
            if (emailMessage.LinkedResources?.Any() == true)
            {
                foreach (var resource in emailMessage.LinkedResources)
                {
                    if (resource.FileStream != null)
                    {
                        var linkedResource = bodyBuilder.LinkedResources.Add(
                            resource.FileName,
                            resource.FileStream,
                            MimeKit.ContentType.Parse(resource.ContentType ?? "application/octet-stream"));
                        linkedResource.ContentId = resource.ContentId;
                    }
                    else if (resource.FileBytes != null)
                    {
                        var linkedResource = bodyBuilder.LinkedResources.Add(
                            resource.FileName,
                            resource.FileBytes,
                            MimeKit.ContentType.Parse(resource.ContentType ?? "application/octet-stream"));
                        linkedResource.ContentId = resource.ContentId;
                    }
                }
            }

            message.Body = bodyBuilder.ToMessageBody();

            return message;
        }

        private async Task SendViaMailKit(MimeMessage message)
        {
            using var smtpClient = new SmtpClient();

            try
            {
                // Configure SMTP client
                smtpClient.ServerCertificateValidationCallback = (sender, certificate, chain, errors) =>
                {
                    // Accept all certificates for development
                    // In production, implement proper certificate validation
                    if (_emailSettings.Smtp.AllowInvalidCertificate)
                    {
                        return true;
                    }
                    return errors == System.Net.Security.SslPolicyErrors.None;
                };

                // Connect to SMTP server
                var host = _emailSettings.Smtp.Host;
                var port = _emailSettings.Smtp.Port;
                var useSsl = _emailSettings.Smtp.UseSsl;
                var useStartTls = _emailSettings.Smtp.UseStartTls;

                if (useStartTls)
                {
                    await smtpClient.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                }
                else if (useSsl)
                {
                    await smtpClient.ConnectAsync(host, port, SecureSocketOptions.SslOnConnect);
                }
                else
                {
                    await smtpClient.ConnectAsync(host, port, SecureSocketOptions.Auto);
                }

                // Authenticate if credentials are provided
                var username = _emailSettings.Smtp.Username;
                var password = _emailSettings.Smtp.Password;

                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    await smtpClient.AuthenticateAsync(username, password);
                }

                // Send email
                var response = await smtpClient.SendAsync(message);
                _logger.LogInformation("Email sent with response: {Response}", response);

                // Disconnect
                await smtpClient.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email via MailKit");
                throw;
            }
        }

        private void ValidateEmailMessage(EmailMessage emailMessage)
        {
            if (emailMessage == null)
                throw new ArgumentNullException(nameof(emailMessage));

            if (emailMessage.To == null || !emailMessage.To.Any())
                throw new ArgumentException("At least one recipient is required", nameof(emailMessage.To));

            if (string.IsNullOrWhiteSpace(emailMessage.Subject))
                throw new ArgumentException("Subject is required", nameof(emailMessage.Subject));

            if (string.IsNullOrWhiteSpace(emailMessage.Body))
                throw new ArgumentException("Body is required", nameof(emailMessage.Body));

            // Validate email addresses
            var allEmails = new List<string>();
            allEmails.AddRange(emailMessage.To);

            if (emailMessage.Cc != null)
                allEmails.AddRange(emailMessage.Cc);

            if (emailMessage.Bcc != null)
                allEmails.AddRange(emailMessage.Bcc);

            if (emailMessage.ReplyTo != null)
                allEmails.AddRange(emailMessage.ReplyTo);

            foreach (var email in allEmails)
            {
                if (!IsValidEmail(email))
                    throw new ArgumentException($"Invalid email address: {email}");
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = MailboxAddress.Parse(email);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> LoadTemplate<T>(string templateName, Dictionary<string, string> data, List<T> tableData = null)
        {
            var templatePath = Path.Combine("EmailTemplates", $"{templateName}.html");

            if (!File.Exists(templatePath))
            {
                return string.Join("<br/>", data.Select(kv => $"{kv.Key}: {kv.Value}"));
            }

            var template = await File.ReadAllTextAsync(templatePath);

            // Replace standard placeholders
            foreach (var item in data)
            {
                template = template.Replace($"{{{{{item.Key}}}}}", item.Value);
            }

            // Replace table placeholder if present
            if (template.Contains("{{TableData}}") && tableData != null)
            {
                string tableHtml = GenerateHtmlTableFromList(tableData);
                template = template.Replace("{{TableData}}", tableHtml);
            }

            return template;
        }

        private string GenerateHtmlTableFromList<T>(List<T> items)
        {
            if (items == null || !items.Any())
                return "<p>No data available</p>";

            var props = typeof(T).GetProperties();
            var sb = new StringBuilder();
            sb.Append("<table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse;'>");

            // Header
            sb.Append("<tr>");
            foreach (var prop in props)
            {
                // Convert property name to readable format
                var displayName = System.Text.RegularExpressions.Regex.Replace(
                    prop.Name,
                    "([A-Z])",
                    " $1").Trim();
                sb.Append($"<th>{displayName}</th>");
            }
            sb.Append("</tr>");

            // Rows
            foreach (var item in items)
            {
                sb.Append("<tr>");
                foreach (var prop in props)
                {
                    var value = prop.GetValue(item)?.ToString() ?? "";
                    sb.Append($"<td>{System.Net.WebUtility.HtmlEncode(value)}</td>");
                }
                sb.Append("</tr>");
            }

            sb.Append("</table>");
            return sb.ToString();
        }
    }
}
