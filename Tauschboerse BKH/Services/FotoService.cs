namespace TauschboerseBKH.Services;

/// <summary>
/// Artikelfotos liegen als Datei im Ordner "Fotos" neben der Datenbank (Netzlaufwerk).
/// In der Datenbank (i_photo) steht nur der Dateiname.
/// </summary>
internal static class FotoService
{
    public const string DateiFilter = "Bilder|*.png;*.jpg;*.jpeg;*.bmp;*.gif";

    private const long MaximaleGroesse = 5 * 1024 * 1024;
    private static readonly string[] ErlaubteEndungen = { ".png", ".jpg", ".jpeg", ".bmp", ".gif" };

    /// <summary>Kopiert das Bild in den Foto-Ordner und liefert den neuen Dateinamen.</summary>
    public static string Speichern(string quelle)
    {
        var endung = Path.GetExtension(quelle).ToLowerInvariant();
        if (!ErlaubteEndungen.Contains(endung))
        {
            throw new InvalidOperationException("Dieser Dateityp wird nicht unterstützt (erlaubt: PNG, JPG, BMP, GIF).");
        }

        if (new FileInfo(quelle).Length > MaximaleGroesse)
        {
            throw new InvalidOperationException("Das Foto ist größer als 5 MB.");
        }

        Directory.CreateDirectory(Konfiguration.FotoOrdner);
        var dateiname = Guid.NewGuid().ToString("N") + endung;
        File.Copy(quelle, Path.Combine(Konfiguration.FotoOrdner, dateiname));
        return dateiname;
    }

    public static Image? Laden(string? dateiname)
    {
        if (string.IsNullOrWhiteSpace(dateiname))
        {
            return null;
        }

        return LadenAusPfad(Path.Combine(Konfiguration.FotoOrdner, Path.GetFileName(dateiname)));
    }

    public static Image? LadenAusPfad(string pfad)
    {
        try
        {
            if (!File.Exists(pfad))
            {
                return null;
            }

            using var datei = File.OpenRead(pfad);
            using var bild = Image.FromStream(datei);
            return new Bitmap(bild);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or OutOfMemoryException)
        {
            return null;
        }
    }

    public static void Loeschen(string? dateiname)
    {
        if (string.IsNullOrWhiteSpace(dateiname))
        {
            return;
        }

        try
        {
            File.Delete(Path.Combine(Konfiguration.FotoOrdner, Path.GetFileName(dateiname)));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Ein nicht löschbares Foto ist kein Grund, die Aktion abzubrechen.
        }
    }
}
