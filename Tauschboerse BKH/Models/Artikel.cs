namespace TauschboerseBKH.Models;

/// <summary>Ein Artikel der Tauschbörse (Tabelle item).</summary>
internal sealed class Artikel
{
    public static readonly IReadOnlyList<string> Stati = new[] { "Verfügbar", "Reserviert", "Verkauft", "Getauscht" };

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Beschreibung { get; set; } = "";
    public decimal Preis { get; set; }
    public DateTime Datum { get; set; } = DateTime.Now;
    public string Status { get; set; } = Stati[0];

    /// <summary>Dateiname des Fotos im Foto-Ordner (neben der Datenbank).</summary>
    public string? Foto { get; set; }

    public int NutzerId { get; set; }
    public List<string> Kategorien { get; set; } = new();

    public string AnbieterName { get; set; } = "";
    public string AnbieterEmail { get; set; } = "";
    public string AnbieterTelefon { get; set; } = "";

    public string KategorienText => string.Join(", ", Kategorien);
}
