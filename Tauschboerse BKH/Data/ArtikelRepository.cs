using System.Data.OleDb;
using System.Text;
using TauschboerseBKH.Models;

namespace TauschboerseBKH.Data;

/// <summary>
/// Zugriff auf item, category und die Auflösungstabelle item_category (n:m).
/// Tabellen- und Spaltennamen folgen dem ER-Modell aus dem Pflichtenheft.
/// </summary>
internal static class ArtikelRepository
{
    private const string Auswahl =
        "SELECT i.i_id, i.i_name, i.i_desc, i.i_price, i.i_date, i.i_status, i.i_photo, i.u_id, " +
        "c.u_title, c.u_fname, c.u_lname, c.u_email, c.u_tel " +
        "FROM item AS i LEFT JOIN contact AS c ON i.u_id = c.u_id";

    public static List<string> AlleKategorien()
    {
        var ergebnis = new List<string>();
        using var verbindung = Datenbank.Oeffnen();
        using var befehl = new OleDbCommand("SELECT ca_name FROM category ORDER BY ca_name", verbindung);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
        {
            ergebnis.Add(leser.Zeichenkette("ca_name"));
        }

        return ergebnis;
    }

    /// <param name="suchtext">Sucht in Bezeichnung und Beschreibung (optional).</param>
    /// <param name="kategorie">Kategoriefilter (optional).</param>
    /// <param name="nutzerId">Nur Artikel dieses Nutzers (optional, für "Meine Artikel").</param>
    public static List<Artikel> Suchen(string? suchtext, string? kategorie, int? nutzerId)
    {
        var sql = new StringBuilder(Auswahl);
        var bedingungen = new List<string>();

        using var verbindung = Datenbank.Oeffnen();
        using var befehl = new OleDbCommand { Connection = verbindung };

        if (!string.IsNullOrWhiteSpace(suchtext))
        {
            var muster = "%" + LikeMaskieren(suchtext.Trim()) + "%";
            bedingungen.Add("(i.i_name LIKE ? OR i.i_desc LIKE ?)");
            befehl.MitText(muster).MitText(muster);
        }

        if (!string.IsNullOrWhiteSpace(kategorie))
        {
            bedingungen.Add("i.i_id IN (SELECT i_id FROM item_category WHERE ca_name = ?)");
            befehl.MitText(kategorie);
        }

        if (nutzerId.HasValue)
        {
            bedingungen.Add("i.u_id = ?");
            befehl.MitZahl(nutzerId.Value);
        }

        if (bedingungen.Count > 0)
        {
            sql.Append(" WHERE ").Append(string.Join(" AND ", bedingungen));
        }

        sql.Append(" ORDER BY i.i_date DESC, i.i_id DESC");
        befehl.CommandText = sql.ToString();

        var artikel = new List<Artikel>();
        using (var leser = befehl.ExecuteReader())
        {
            while (leser.Read())
            {
                artikel.Add(Lesen(leser));
            }
        }

        KategorienZuordnen(verbindung, artikel);
        return artikel;
    }

    public static void Hinzufuegen(Artikel artikel)
    {
        using var verbindung = Datenbank.Oeffnen();
        using var transaktion = verbindung.BeginTransaction();

        Datenbank.Ausfuehren(
            new OleDbCommand(
                "INSERT INTO item (i_name, i_desc, i_price, i_date, i_status, i_photo, u_id) VALUES (?, ?, ?, ?, ?, ?, ?)",
                verbindung, transaktion)
                .MitText(artikel.Name)
                .MitMemo(artikel.Beschreibung)
                .MitWaehrung(artikel.Preis)
                .MitDatum(artikel.Datum)
                .MitText(artikel.Status)
                .MitText(artikel.Foto)
                .MitZahl(artikel.NutzerId));

        artikel.Id = Convert.ToInt32(Datenbank.Skalar(new OleDbCommand("SELECT @@IDENTITY", verbindung, transaktion)));

        KategorienSpeichern(verbindung, transaktion, artikel);
        transaktion.Commit();
    }

    /// <summary>Ändert einen Artikel. Nur der Besitzer (u_id) darf ändern.</summary>
    public static void Aendern(Artikel artikel)
    {
        using var verbindung = Datenbank.Oeffnen();
        using var transaktion = verbindung.BeginTransaction();

        var betroffen = Datenbank.Ausfuehren(
            new OleDbCommand(
                "UPDATE item SET i_name = ?, i_desc = ?, i_price = ?, i_status = ?, i_photo = ? WHERE i_id = ? AND u_id = ?",
                verbindung, transaktion)
                .MitText(artikel.Name)
                .MitMemo(artikel.Beschreibung)
                .MitWaehrung(artikel.Preis)
                .MitText(artikel.Status)
                .MitText(artikel.Foto)
                .MitZahl(artikel.Id)
                .MitZahl(artikel.NutzerId));

        if (betroffen != 1)
        {
            throw new InvalidOperationException("Der Artikel wurde nicht gefunden oder gehört einem anderen Nutzer.");
        }

        Datenbank.Ausfuehren(
            new OleDbCommand("DELETE FROM item_category WHERE i_id = ?", verbindung, transaktion).MitZahl(artikel.Id));

        KategorienSpeichern(verbindung, transaktion, artikel);
        transaktion.Commit();
    }

    /// <summary>Löscht einen Artikel. Nur der Besitzer (u_id) darf löschen.</summary>
    public static void Loeschen(int artikelId, int nutzerId)
    {
        using var verbindung = Datenbank.Oeffnen();
        using var transaktion = verbindung.BeginTransaction();

        var gehoertNutzer = Convert.ToInt32(Datenbank.Skalar(
            new OleDbCommand("SELECT COUNT(*) FROM item WHERE i_id = ? AND u_id = ?", verbindung, transaktion)
                .MitZahl(artikelId)
                .MitZahl(nutzerId)));
        if (gehoertNutzer != 1)
        {
            throw new InvalidOperationException("Der Artikel wurde nicht gefunden oder gehört einem anderen Nutzer.");
        }

        Datenbank.Ausfuehren(
            new OleDbCommand("DELETE FROM item_category WHERE i_id = ?", verbindung, transaktion).MitZahl(artikelId));
        Datenbank.Ausfuehren(
            new OleDbCommand("DELETE FROM item WHERE i_id = ? AND u_id = ?", verbindung, transaktion)
                .MitZahl(artikelId)
                .MitZahl(nutzerId));

        transaktion.Commit();
    }

    private static Artikel Lesen(OleDbDataReader leser)
    {
        var vorname = leser.Zeichenkette("u_fname");
        var nachname = leser.Zeichenkette("u_lname");

        return new Artikel
        {
            Id = Convert.ToInt32(leser["i_id"]),
            Name = leser.Zeichenkette("i_name"),
            Beschreibung = leser.Zeichenkette("i_desc"),
            Preis = leser["i_price"] is DBNull ? 0m : Convert.ToDecimal(leser["i_price"]),
            Datum = leser["i_date"] is DBNull ? DateTime.MinValue : Convert.ToDateTime(leser["i_date"]),
            Status = leser.Zeichenkette("i_status"),
            Foto = leser["i_photo"] is DBNull ? null : leser.Zeichenkette("i_photo"),
            NutzerId = Convert.ToInt32(leser["u_id"]),
            AnbieterName = $"{vorname} {nachname}".Trim(),
            AnbieterEmail = leser.Zeichenkette("u_email"),
            AnbieterTelefon = leser.Zeichenkette("u_tel"),
        };
    }

    private static void KategorienZuordnen(OleDbConnection verbindung, List<Artikel> artikel)
    {
        if (artikel.Count == 0)
        {
            return;
        }

        var nachId = artikel.ToDictionary(a => a.Id);
        using var befehl = new OleDbCommand("SELECT i_id, ca_name FROM item_category ORDER BY ca_name", verbindung);
        using var leser = befehl.ExecuteReader();
        while (leser.Read())
        {
            if (nachId.TryGetValue(Convert.ToInt32(leser["i_id"]), out var a))
            {
                a.Kategorien.Add(leser.Zeichenkette("ca_name"));
            }
        }
    }

    /// <summary>Legt neue Kategorien an und verknüpft den Artikel über item_category.</summary>
    private static void KategorienSpeichern(OleDbConnection verbindung, OleDbTransaction transaktion, Artikel artikel)
    {
        foreach (var kategorie in artikel.Kategorien.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var vorhanden = Convert.ToInt32(Datenbank.Skalar(
                new OleDbCommand("SELECT COUNT(*) FROM category WHERE ca_name = ?", verbindung, transaktion)
                    .MitText(kategorie)));

            if (vorhanden == 0)
            {
                Datenbank.Ausfuehren(
                    new OleDbCommand("INSERT INTO category (ca_name, ca_desc) VALUES (?, ?)", verbindung, transaktion)
                        .MitText(kategorie)
                        .MitText(null));
            }

            Datenbank.Ausfuehren(
                new OleDbCommand("INSERT INTO item_category (i_id, ca_name) VALUES (?, ?)", verbindung, transaktion)
                    .MitZahl(artikel.Id)
                    .MitText(kategorie));
        }
    }

    /// <summary>Verhindert, dass Zeichen aus der Sucheingabe als LIKE-Platzhalter wirken.</summary>
    private static string LikeMaskieren(string text)
        => text.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
