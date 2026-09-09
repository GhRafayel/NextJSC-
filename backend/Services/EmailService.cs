using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Backend.Options;

namespace Backend.Services;

public class EmailService(IOptions<EmailOptions> options)
{
    private readonly EmailOptions _opt = options.Value;
    public async Task SendAsync (string to, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_opt.From));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body  = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        client.CheckCertificateRevocation = false;
        await client.ConnectAsync(_opt.Host, _opt.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_opt.User, _opt.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

    }
}