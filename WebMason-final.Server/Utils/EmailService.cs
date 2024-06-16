using Microsoft.Extensions.Options;
using MimeKit;
//using MailKit.Net.Smtp;
using System;
using System.Net;
using System.Net.Mail;

namespace WebMason_final.Server.Utils
{
    public class EmailService
    {
        private readonly SmtpSettings _smtpSettings;

        public EmailService(IOptions<SmtpSettings> smtpSettings)
        {
            _smtpSettings = smtpSettings.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            //var message = new MimeMessage();
            //message.From.Add(new MailboxAddress(_smtpSettings.SenderName, _smtpSettings.SenderEmail));
            //message.To.Add(new MailboxAddress("", toEmail));
            //message.Subject = subject;
            //message.Body = new TextPart("html")
            //{
            //    Text = body
            //};

            //using var client = new SmtpClient();
            //try
            //{
            //    await client.ConnectAsync(_smtpSettings.Server, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
            //    await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
            //    await client.SendAsync(message);
            //}
            //finally
            //{
            //    await client.DisconnectAsync(true);
            //    client.Dispose();
            //}
            MailMessage mailMessage = new MailMessage(new MailAddress(_smtpSettings.SenderEmail),new MailAddress(toEmail));
            mailMessage.From = new MailAddress("mathisbureau@gmail.com");
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = subject;
            mailMessage.Body = body;

            SmtpClient smtpClient = new SmtpClient(_smtpSettings.Server);
            smtpClient.Host = _smtpSettings.Server;
            smtpClient.Port = 587;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
            smtpClient.EnableSsl = true;

            try
            {
                smtpClient.Send(mailMessage);
                Console.WriteLine("Email Sent Successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }

    public class SmtpSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string SenderName { get; set; }
        public string SenderEmail { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

}
