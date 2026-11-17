using System.Data.OleDb;
using TauschboerseBKH.Models;
using TauschboerseBKH.Services;

namespace TauschboerseBKH.Data;

/// <summary>Registrierung und Anmeldung (Tabellen user und contact).</summary>
internal static class NutzerRepository
{
    /// <returns><c>false</c>, wenn die E-Mail-Adresse bereits registriert ist.</returns>
    public static bool Registrieren(Nutzer nutzer, string passwort)
    {
        var hash = PasswortHasher.Hashen(passwort);

        using var verbindung = Datenbank.Oeffnen();
        using var transaktion = verbindung.BeginTransaction();

        var vorhanden = Convert.ToInt32(Datenbank.Skalar(
            new OleDbCommand("SELECT COUNT(*) FROM [user] WHERE u_email = ?", verbindung, transaktion)
                .MitText(nutzer.Email)));
        if (vorhanden > 0)
        {
            return false;
        }

        Datenbank.Ausfuehren(
            new OleDbCommand("INSERT INTO [user] (u_email, u_passwd) VALUES (?, ?)", verbindung, transaktion)
                .MitText(nutzer.Email)
                .MitText(hash));

        nutzer.Id = Convert.ToInt32(Datenbank.Skalar(new OleDbCommand("SELECT @@IDENTITY", verbindung, transaktion)));

        Datenbank.Ausfuehren(
            new OleDbCommand(
                "INSERT INTO contact (u_id, u_title, u_fname, u_lname, u_email, u_tel) VALUES (?, ?, ?, ?, ?, ?)",
                verbindung, transaktion)
                .MitZahl(nutzer.Id)
                .MitText(nutzer.Anrede)
                .MitText(nutzer.Vorname)
                .MitText(nutzer.Nachname)
                .MitText(nutzer.Email)
                .MitText(nutzer.Telefon));

        transaktion.Commit();
        return true;
    }

    /// <returns>Der Nutzer oder <c>null</c>, wenn E-Mail oder Passwort nicht stimmen.</returns>
    public static Nutzer? Anmelden(string email, string passwort)
    {
        using var verbindung = Datenbank.Oeffnen();
        using var befehl = new OleDbCommand(
            "SELECT u.u_id, u.u_email, u.u_passwd, c.u_title, c.u_fname, c.u_lname, c.u_tel " +
            "FROM [user] AS u LEFT JOIN contact AS c ON u.u_id = c.u_id " +
            "WHERE u.u_email = ?", verbindung)
            .MitText(email);

        using var leser = befehl.ExecuteReader();
        if (!leser.Read() || !PasswortHasher.Pruefen(passwort, leser.Zeichenkette("u_passwd")))
        {
            return null;
        }

        return new Nutzer
        {
            Id = Convert.ToInt32(leser["u_id"]),
            Email = leser.Zeichenkette("u_email"),
            Anrede = leser.Zeichenkette("u_title"),
            Vorname = leser.Zeichenkette("u_fname"),
            Nachname = leser.Zeichenkette("u_lname"),
            Telefon = leser.Zeichenkette("u_tel"),
        };
    }
}
