using System.Data.OleDb;

namespace TauschboerseBKH.Data;

/// <summary>Verbindung zur Access-Datenbank (OleDb) und kleine Hilfsmethoden.</summary>
internal static class Datenbank
{
    public static OleDbConnection Oeffnen()
    {
        var pfad = Konfiguration.DatenbankPfad;
        if (!File.Exists(pfad))
        {
            throw new FileNotFoundException($"Die Datenbank wurde nicht gefunden:\n{pfad}");
        }

        var builder = new OleDbConnectionStringBuilder
        {
            Provider = Konfiguration.DatenbankProvider,
            DataSource = pfad,
        };

        var verbindung = new OleDbConnection(builder.ConnectionString);
        try
        {
            verbindung.Open();
        }
        catch
        {
            verbindung.Dispose();
            throw;
        }

        return verbindung;
    }

    public static int Ausfuehren(OleDbCommand befehl)
    {
        using (befehl)
        {
            return befehl.ExecuteNonQuery();
        }
    }

    public static object? Skalar(OleDbCommand befehl)
    {
        using (befehl)
        {
            return befehl.ExecuteScalar();
        }
    }
}

/// <summary>
/// Parameter werden immer als Parameter übergeben (kein String-Zusammenbau) -> kein SQL-Injection-Risiko.
/// OleDb arbeitet mit Platzhaltern "?" in der Reihenfolge der Parameter.
/// </summary>
internal static class OleDbErweiterungen
{
    public static OleDbCommand MitText(this OleDbCommand befehl, string? wert)
    {
        befehl.Parameters.Add("?", OleDbType.VarWChar).Value = string.IsNullOrEmpty(wert) ? DBNull.Value : wert;
        return befehl;
    }

    public static OleDbCommand MitMemo(this OleDbCommand befehl, string? wert)
    {
        befehl.Parameters.Add("?", OleDbType.LongVarWChar).Value = string.IsNullOrEmpty(wert) ? DBNull.Value : wert;
        return befehl;
    }

    public static OleDbCommand MitZahl(this OleDbCommand befehl, int wert)
    {
        befehl.Parameters.Add("?", OleDbType.Integer).Value = wert;
        return befehl;
    }

    public static OleDbCommand MitWaehrung(this OleDbCommand befehl, decimal wert)
    {
        befehl.Parameters.Add("?", OleDbType.Currency).Value = wert;
        return befehl;
    }

    public static OleDbCommand MitDatum(this OleDbCommand befehl, DateTime wert)
    {
        befehl.Parameters.Add("?", OleDbType.Date).Value = wert;
        return befehl;
    }

    public static string Zeichenkette(this OleDbDataReader leser, string spalte)
        => leser[spalte] is DBNull ? "" : Convert.ToString(leser[spalte]) ?? "";
}
