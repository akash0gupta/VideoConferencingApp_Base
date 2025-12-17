using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using VideoConferencingApp.Application.DTOs.Notification;

namespace VideoConferencingApp.Application.Common.INotificationServices
{
    public interface IEmailService
    {
        Task SendEmailAsync(EmailMessage emailMessage);
        Task SendEmailAsync(string to, string subject, string body);
        Task SendTemplatedEmailAsync<T>(string to, string subject, string templateName, Dictionary<string, string> data, List<T> tableData = null);
        Task SendTemplatedEmailAsync<T>(EmailTemplateMessage<T> templateMessage);
      
    }
}
