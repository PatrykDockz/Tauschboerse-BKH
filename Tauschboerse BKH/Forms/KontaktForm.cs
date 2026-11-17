using TauschboerseBKH.Models;
using TauschboerseBKH.Services;

namespace TauschboerseBKH.Forms;

/// <summary>Mockup "Kontakt – Verkäufer kontaktieren". Versendet die Nachricht per Mail.</summary>
internal sealed class KontaktForm : Form
{
    private readonly Artikel artikel;
    private readonly TextBox txtVerkaeufer = new() { ReadOnly = true };
    private readonly TextBox txtKaeufer = new() { MaxLength = 100 };
    private readonly TextBox txtNachricht = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
    private readonly TextBox txtTelVerkaeufer = new() { ReadOnly = true };

    public KontaktForm(Artikel artikel)
    {
        this.artikel = artikel;
        var nutzer = Sitzung.Nutzer;

        txtVerkaeufer.Text = artikel.AnbieterEmail;
        txtTelVerkaeufer.Text = artikel.AnbieterTelefon;
        txtKaeufer.Text = nutzer?.Email ?? "";
        txtKaeufer.ReadOnly = nutzer is not null; // Gäste tragen ihre Adresse selbst ein
        txtNachricht.Text = Vorlage(nutzer);

        var tabelle = Ui.Formular();
        Ui.Ueberschrift(tabelle, "Verkäufer kontaktieren");
        Ui.Zeile(tabelle, "Verkäufer", txtVerkaeufer);
        Ui.Zeile(tabelle, "Käufer", txtKaeufer);
        Ui.Zeile(tabelle, "Nachricht", txtNachricht, 200);
        Ui.Zeile(tabelle, "Tel.-Verkäufer", txtTelVerkaeufer);

        var abbrechen = Ui.Knopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        var bestaetigen = Ui.Knopf("Bestätigen");
        bestaetigen.Click += (_, _) => Senden();

        Ui.Dialog(this, "Verkäufer kontaktieren", new Size(520, 480), tabelle, Ui.Knopfleiste(null, abbrechen, bestaetigen));
        CancelButton = abbrechen;
    }

    /// <summary>Vorbelegte Nachricht mit Artikelname, Artikelnummer, Preis und Telefonnummer des Käufers.</summary>
    private string Vorlage(Nutzer? kaeufer)
    {
        var zeilen = new[]
        {
            $"Hallo {artikel.AnbieterName},",
            "",
            "ich interessiere mich für folgenden Artikel:",
            $"Artikel: {artikel.Name}",
            $"Artikel-Nr.: {artikel.Id}",
            $"Preis: {Ui.Preis(artikel.Preis)}",
            $"Meine Telefonnummer: {kaeufer?.Telefon}",
            "",
            "Viele Grüße",
            kaeufer?.VollerName ?? "",
        };
        return string.Join(Environment.NewLine, zeilen);
    }

    private void Senden()
    {
        var kaeufer = txtKaeufer.Text.Trim();
        if (!MailService.IstGueltigeAdresse(kaeufer))
        {
            Ui.Hinweis(this, "Bitte eine gültige E-Mail-Adresse für den Käufer angeben.");
            return;
        }

        if (!MailService.IstGueltigeAdresse(artikel.AnbieterEmail))
        {
            Ui.Fehler(this, "Für diesen Anbieter ist keine gültige E-Mail-Adresse hinterlegt.");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtNachricht.Text))
        {
            Ui.Hinweis(this, "Bitte eine Nachricht eingeben.");
            return;
        }

        var text = txtNachricht.Text.Trim() +
                   $"{Environment.NewLine}{Environment.NewLine}---{Environment.NewLine}" +
                   $"Gesendet über die Tauschbörse BK-Hilden. Antworten an: {kaeufer}";

        try
        {
            Cursor = Cursors.WaitCursor;
            MailService.Senden(artikel.AnbieterEmail, kaeufer, $"Tauschbörse: Anfrage zu \"{artikel.Name}\" (Nr. {artikel.Id})", text);
            Ui.Hinweis(this, "Die Nachricht wurde an den Verkäufer gesendet.");
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            Ui.Fehler(this, "Die Nachricht konnte nicht gesendet werden:\n" + ex.Message);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }
}
