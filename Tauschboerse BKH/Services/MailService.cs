using System.Net;
using System.Net.Mail;
using System.Text;

namespace TauschboerseBKH.Services;

/// <summary>Versand der Nachrichten aus dem Kontaktformular per SMTP.</summary>
internal static class MailService
{
    public static bool IstGueltigeAdresse(string adresse)
    {
        try
        {
            return new MailAddress(adresse).Address == adresse;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <param name="an">Empfänger (Verkäufer)</param>
    /// <param name="antwortAn">Adresse des Käufers, an die geantwortet werden soll</param>
    public static void Senden(string an, string antwortAn, string betreff, string text)
    {
        if (string.IsNullOrWhiteSpace(Konfiguration.SmtpHost))
        {
            throw new InvalidOperationException("Der Mailserver ist nicht konfiguriert (App.config, Einstellung \"SmtpHost\").");
        }

        using var nachricht = new MailMessage
        {
            From = new MailAddress(Konfiguration.MailAbsender),
            Subject = betreff.Replace('\r', ' ').Replace('\n', ' '),
            SubjectEncoding = Encoding.UTF8,
            Body = text,
            BodyEncoding = Encoding.UTF8,
        };
        nachricht.To.Add(an);
        nachricht.ReplyToList.Add(antwortAn);

        using var client = new SmtpClient(Konfiguration.SmtpHost, Konfiguration.SmtpPort)
        {
            EnableSsl = Konfiguration.SmtpSsl,
            Timeout = 15000,
        };

        if (!string.IsNullOrEmpty(Konfiguration.SmtpBenutzer))
        {
            client.Credentials = new NetworkCredential(Konfiguration.SmtpBenutzer, Konfiguration.SmtpPasswort);
        }

        client.Send(nachricht);
    }
}
