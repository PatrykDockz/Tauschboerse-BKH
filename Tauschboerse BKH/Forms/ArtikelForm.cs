using System.Globalization;
using TauschboerseBKH.Data;
using TauschboerseBKH.Models;
using TauschboerseBKH.Services;

namespace TauschboerseBKH.Forms;

/// <summary>Mockup "Artikel": neuen Artikel anlegen oder bestehenden bearbeiten.</summary>
internal sealed class ArtikelForm : Form
{
    private readonly Artikel? bestehend;
    private readonly TextBox txtName = new() { MaxLength = 100 };
    private readonly TextBox txtBeschreibung = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
    private readonly ComboBox cmbKategorie = new() { DropDownStyle = ComboBoxStyle.DropDown, MaxLength = 50 };
    private readonly TextBox txtPreis = new() { MaxLength = 12 };
    private readonly PictureBox bild = new() { SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle, Size = new Size(110, 110) };
    private readonly ComboBox cmbStatus = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private string? neuesFoto;

    /// <param name="bestehend"><c>null</c> = neuer Artikel.</param>
    public ArtikelForm(Artikel? bestehend)
    {
        this.bestehend = bestehend;

        foreach (var status in Artikel.Stati)
        {
            cmbStatus.Items.Add(status);
        }

        try
        {
            foreach (var kategorie in ArtikelRepository.AlleKategorien())
            {
                cmbKategorie.Items.Add(kategorie);
            }
        }
        catch (Exception ex)
        {
            Ui.Fehler(this, "Die Kategorien konnten nicht geladen werden:\n" + ex.Message);
        }

        if (bestehend is null)
        {
            cmbStatus.SelectedIndex = 0;
        }
        else
        {
            txtName.Text = bestehend.Name;
            txtBeschreibung.Text = bestehend.Beschreibung;
            cmbKategorie.Text = bestehend.Kategorien.FirstOrDefault() ?? "";
            txtPreis.Text = bestehend.Preis.ToString("N2", Ui.Kultur);
            cmbStatus.SelectedItem = bestehend.Status;
            if (cmbStatus.SelectedIndex < 0)
            {
                cmbStatus.SelectedIndex = 0;
            }

            bild.Image = FotoService.Laden(bestehend.Foto);
        }

        var fotoWaehlen = Ui.Knopf("Foto wählen...", 120);
        fotoWaehlen.Click += (_, _) => FotoWaehlen();
        var fotoZeile = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        fotoZeile.Controls.Add(bild);
        fotoZeile.Controls.Add(fotoWaehlen);

        var tabelle = Ui.Formular();
        Ui.Ueberschrift(tabelle, "Artikel");
        Ui.Zeile(tabelle, "Name", txtName);
        Ui.Zeile(tabelle, "Beschreibung", txtBeschreibung, 130);
        Ui.Zeile(tabelle, "Kategorie", cmbKategorie);
        Ui.Zeile(tabelle, "Preis (€)", txtPreis);
        Ui.Zeile(tabelle, "Photo", fotoZeile);
        Ui.Zeile(tabelle, "Status", cmbStatus);

        var abbrechen = Ui.Knopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        var bestaetigen = Ui.Knopf("Bestätigen");
        bestaetigen.Click += (_, _) => Bestaetigen();

        Ui.Dialog(this, bestehend is null ? "Neuer Artikel" : "Artikel bearbeiten", new Size(500, 600), tabelle,
            Ui.Knopfleiste(null, abbrechen, bestaetigen));
        CancelButton = abbrechen;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            bild.Image?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void FotoWaehlen()
    {
        using var dialog = new OpenFileDialog { Filter = FotoService.DateiFilter, Title = "Foto auswählen" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var geladen = FotoService.LadenAusPfad(dialog.FileName);
        if (geladen is null)
        {
            Ui.Fehler(this, "Das ausgewählte Bild konnte nicht geladen werden.");
            return;
        }

        var alt = bild.Image;
        bild.Image = geladen;
        alt?.Dispose();
        neuesFoto = dialog.FileName;
    }

    private void Bestaetigen()
    {
        var name = txtName.Text.Trim();
        var kategorie = cmbKategorie.Text.Trim();

        if (name.Length == 0)
        {
            Ui.Hinweis(this, "Bitte eine Bezeichnung eingeben.");
            return;
        }

        if (kategorie.Length == 0)
        {
            Ui.Hinweis(this, "Bitte eine Kategorie auswählen oder neu eingeben.");
            return;
        }

        if (!decimal.TryParse(txtPreis.Text.Replace("€", "").Trim(), NumberStyles.Number, Ui.Kultur, out var preis)
            || preis < 0 || preis > 1_000_000m)
        {
            Ui.Hinweis(this, "Bitte einen gültigen Preis eingeben (z. B. 12,50).");
            return;
        }

        var artikel = new Artikel
        {
            Id = bestehend?.Id ?? 0,
            Name = name,
            Beschreibung = txtBeschreibung.Text.Trim(),
            Preis = preis,
            Status = (string)cmbStatus.SelectedItem!,
            Foto = bestehend?.Foto,
            NutzerId = Sitzung.Nutzer!.Id,
            Datum = bestehend?.Datum ?? DateTime.Now,
            Kategorien = { kategorie },
        };

        string? neuGespeichertesFoto = null;
        try
        {
            string? altesFoto = null;
            if (neuesFoto is not null)
            {
                altesFoto = bestehend?.Foto;
                neuGespeichertesFoto = FotoService.Speichern(neuesFoto);
                artikel.Foto = neuGespeichertesFoto;
            }

            if (bestehend is null)
            {
                ArtikelRepository.Hinzufuegen(artikel);
            }
            else
            {
                ArtikelRepository.Aendern(artikel);
            }

            FotoService.Loeschen(altesFoto);
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            FotoService.Loeschen(neuGespeichertesFoto);
            Ui.Fehler(this, "Der Artikel konnte nicht gespeichert werden:\n" + ex.Message);
        }
    }
}
