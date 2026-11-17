namespace TauschboerseBKH.Models;

/// <summary>Angemeldeter Teilnehmer (Tabellen user + contact).</summary>
internal sealed class Nutzer
{
    public int Id { get; set; }
    public string Email { get; set; } = "";
    public string Anrede { get; set; } = "";
    public string Vorname { get; set; } = "";
    public string Nachname { get; set; } = "";
    public string Telefon { get; set; } = "";

    public string VollerName => $"{Vorname} {Nachname}".Trim();
}
