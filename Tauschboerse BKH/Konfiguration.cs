using System.Configuration;

namespace TauschboerseBKH;

/// <summary>Zugriff auf die Einstellungen aus der App.config.</summary>
internal static class Konfiguration
{
    private static string Lesen(string schluessel, string standard = "")
        => ConfigurationManager.AppSettings[schluessel] ?? standard;

    public static string DatenbankPfad
    {
        get
        {
            var pfad = Lesen("DatenbankPfad", "Tauschboerse.accdb");
            return Path.IsPathRooted(pfad) ? pfad : Path.Combine(AppContext.BaseDirectory, pfad);
        }
    }

    public static string DatenbankProvider => Lesen("DatenbankProvider", "Microsoft.ACE.OLEDB.12.0");

    /// <summary>Ordner für Artikelfotos (liegt neben der Datenbank auf dem Netzlaufwerk).</summary>
    public static string FotoOrdner => Path.Combine(Path.GetDirectoryName(DatenbankPfad) ?? AppContext.BaseDirectory, "Fotos");

    public static string SmtpHost => Lesen("SmtpHost");
    public static int SmtpPort => int.TryParse(Lesen("SmtpPort"), out var port) ? port : 25;
    public static bool SmtpSsl => !bool.TryParse(Lesen("SmtpSsl"), out var ssl) || ssl;
    public static string SmtpBenutzer => Lesen("SmtpBenutzer");
    public static string SmtpPasswort => Lesen("SmtpPasswort");
    public static string MailAbsender => Lesen("MailAbsender");
}
