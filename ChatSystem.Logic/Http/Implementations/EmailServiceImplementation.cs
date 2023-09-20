using System.Collections;
using System.Net;
using System.Reflection;
using System.Text;
using ChatSystem.Logic.Abstractions;
using ChatSystem.Logic.Constants;
using ChatSystem.Logic.Constants.Configs;
using ChatSystem.Logic.Helpers;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using ILogger = Serilog.ILogger;


namespace ChatSystem.Logic.Email.Implementations;

public class EmailServiceImplementation : IEmailService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly SmtpClient _smtpClient = new SmtpClient();
    private readonly ILogger _logger;

    public EmailServiceImplementation(IOptions<SmtpSettings> smtpSettingsOptions, ILogger logger)
    {
        _logger = logger;
        _smtpSettings = smtpSettingsOptions.Value;

        _smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
        /*_smtpClient.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
        _smtpClient.Authenticate(_smtpSettings.SenderEmail, _smtpSettings.SenderPassword);*/
    }

    private async Task<bool> SendAsync(string recipientEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
        message.To.Add(new MailboxAddress("", recipientEmail));
        message.Subject = subject;

        var builder = new BodyBuilder
        {
            HtmlBody = body
        };

        try
        {
            message.Body = builder.ToMessageBody();
            await _smtpClient.SendAsync(message);

            _logger.Debug("Email sent successfully to {RecipientEmail}", recipientEmail);
            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, "EmailServiceImplementation:SendAsync Failed to send email to {RecipientEmail}", recipientEmail);
            return false;
        }
    }

    public async Task FetchEmailAndSend<T>(EmailTemplateType templateType, T template, string recipient)
    {
        _logger.Debug(
            "EmailServiceImplementation:FetchEmailAndSend - Start(EmailTemplateType: {EmailTemplateType}, Template: {Template}, Recipient: {Recipient})"
            , templateType, template, recipient);

        string templateFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            $"Http/Email/Templates/{(int)templateType}.html");
        if (template is null || !File.Exists(templateFileName))
        {
            _logger.Error("Template not found or null for type {TemplateType}", templateType);
            return;
        }

        string templateContent = await File.ReadAllTextAsync(templateFileName);

        PropertyInfo[] templateProperties =
            template.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in templateProperties)
        {
            object? propertyValue = property.GetValue(template);

            if (propertyValue is not null)
                templateContent = templateContent.Replace(property.Name, propertyValue.ToString());
        }

        //await SendAsync(recipient, templateType.Convert(), templateContent);
    }
}