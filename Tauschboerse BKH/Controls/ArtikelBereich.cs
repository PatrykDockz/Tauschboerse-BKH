using TauschboerseBKH.Data;
using TauschboerseBKH.Models;
using TauschboerseBKH.Services;

namespace TauschboerseBKH.Controls;

/// <summary>
/// Gemeinsame Basis der Reiter "Suchen" und "Meine Artikel": Suchfeld, Kategoriefilter,
/// Artikelliste und (für "Suchen") die Detailansicht.
/// </summary>
internal abstract class ArtikelBereich : UserControl
{
    private const string AlleKategorien = "Alle Kategorien";

    private readonly bool nurEigene;
    private readonly TextBox txtSuche = new();
    private readonly ComboBox cmbFilter = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label lblUeberschrift = new() { AutoSize = true, Font = Ui.GrosseSchrift(12f), Margin = new Padding(12, 10, 3, 6) };
    private readonly ListView liste = new() { View = View.Details, FullRowSelect = true, MultiSelect = false, HideSelection = false, GridLines = true };
    private readonly TableLayoutPanel details = new() { ColumnCount = 2, Visible = false, Padding = new Padding(10) };
    private readonly TextBox txtBeschreibung = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Margin = new Padding(3, 3, 12, 3) };
    private readonly PictureBox bild = new() { SizeMode = PictureBoxSizeMode.Zoom, BorderStyle = BorderStyle.FixedSingle };
    private readonly Label lblAnbieter = DetailLabel();
    private readonly Label lblPreis = DetailLabel();
    private readonly Label lblDatum = DetailLabel();
    private readonly Label lblStatus = DetailLabel();
    private readonly Label lblKategorie = DetailLabel();
    private readonly System.Windows.Forms.Timer suchTimer = new() { Interval = 300 };

    private List<Artikel> artikel = new();
    private bool kategorienGeladen;
    private bool filterWirdGeladen;

    /// <summary>Platz für die Buttons am unteren Rand (rechtsbündig).</summary>
    private readonly FlowLayoutPanel knopfLeiste = new()
    {
        AutoSize = true,
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.RightToLeft,
        Padding = new Padding(8),
    };

    /// <param name="nurEigene"><c>true</c> = nur Artikel des angemeldeten Nutzers anzeigen.</param>
    protected ArtikelBereich(bool nurEigene)
    {
        this.nurEigene = nurEigene;
        Dock = DockStyle.Fill;
        lblUeberschrift.Text = "Artikelliste";

        // Suche und Filter
        var suche = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(10, 10, 10, 0) };
        suche.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        suche.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320));
        Ui.Zeile(suche, "Suche", txtSuche);
        Ui.Zeile(suche, "Filter", cmbFilter);

        // Liste
        liste.Columns.Add("Bezeichnung", 240);
        liste.Columns.Add("Kategorie", 140);
        liste.Columns.Add("Preis", 90, HorizontalAlignment.Right);
        liste.Columns.Add("Status", 100);
        liste.Columns.Add("Eingestellt am", 110);
        if (!nurEigene)
        {
            liste.Columns.Add("Anbieter", 160);
        }

        liste.Dock = DockStyle.Fill;
        liste.DoubleClick += (_, _) => ArtikelDoppelklick();

        DetailsAufbauen();

        var inhalt = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 0, 10, 0) };
        inhalt.Controls.Add(liste);
        inhalt.Controls.Add(details);

        var wurzel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        wurzel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        wurzel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        wurzel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        wurzel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        wurzel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        wurzel.Controls.Add(suche, 0, 0);
        wurzel.Controls.Add(lblUeberschrift, 0, 1);
        wurzel.Controls.Add(inhalt, 0, 2);
        wurzel.Controls.Add(knopfLeiste, 0, 3);
        Controls.Add(wurzel);

        txtSuche.TextChanged += (_, _) =>
        {
            suchTimer.Stop();
            suchTimer.Start();
        };
        suchTimer.Tick += (_, _) =>
        {
            suchTimer.Stop();
            SucheGeaendert();
        };
        cmbFilter.SelectedIndexChanged += (_, _) =>
        {
            if (!filterWirdGeladen)
            {
                SucheGeaendert();
            }
        };
    }

    /// <summary>Der in der Liste markierte Artikel (oder <c>null</c>).</summary>
    protected Artikel? Ausgewaehlt
        => liste.SelectedItems.Count > 0 ? liste.SelectedItems[0].Tag as Artikel : null;

    /// <summary>Fügt Buttons ein (von links nach rechts angegeben, rechtsbündig dargestellt).</summary>
    protected void KnoepfeHinzufuegen(params Button[] linksNachRechts)
    {
        foreach (var knopf in linksNachRechts.Reverse())
        {
            knopfLeiste.Controls.Add(knopf);
        }
    }

    protected virtual void ArtikelDoppelklick()
    {
    }

    public virtual void ZeigeListe()
    {
        lblUeberschrift.Text = "Artikelliste";
        details.Visible = false;
        liste.Visible = true;
    }

    public virtual void ZeigeDetails(Artikel a)
    {
        lblUeberschrift.Text = a.Name;
        txtBeschreibung.Text = a.Beschreibung;
        lblAnbieter.Text = "Anbieter: " + a.AnbieterName;
        lblPreis.Text = "Preis: " + Ui.Preis(a.Preis);
        lblDatum.Text = "Einstellungsdatum: " + a.Datum.ToString("dd.MM.yyyy", Ui.Kultur);
        lblStatus.Text = "Status: " + a.Status;
        lblKategorie.Text = "Kategorie: " + a.KategorienText;

        var altesBild = bild.Image;
        bild.Image = FotoService.Laden(a.Foto);
        altesBild?.Dispose();

        liste.Visible = false;
        details.Visible = true;
    }

    /// <summary>Lädt die Artikelliste anhand von Suchtext und Filter neu.</summary>
    public void Aktualisieren(bool kategorienNeuLaden = false)
    {
        try
        {
            if (kategorienNeuLaden || !kategorienGeladen)
            {
                KategorienLaden();
            }

            var kategorie = cmbFilter.SelectedIndex > 0 ? cmbFilter.SelectedItem as string : null;
            var nutzerId = nurEigene ? Sitzung.Nutzer?.Id : null;

            artikel = nurEigene && nutzerId is null
                ? new List<Artikel>()
                : ArtikelRepository.Suchen(txtSuche.Text, kategorie, nutzerId);

            ListeFuellen();
        }
        catch (Exception ex)
        {
            Ui.Fehler(FindForm(), "Die Artikel konnten nicht geladen werden:\n" + ex.Message);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            suchTimer.Dispose();
            bild.Image?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void SucheGeaendert()
    {
        ZeigeListe();
        Aktualisieren();
    }

    private void KategorienLaden()
    {
        var auswahl = cmbFilter.SelectedItem as string;
        filterWirdGeladen = true;
        try
        {
            cmbFilter.Items.Clear();
            cmbFilter.Items.Add(AlleKategorien);
            foreach (var kategorie in ArtikelRepository.AlleKategorien())
            {
                cmbFilter.Items.Add(kategorie);
            }

            var index = auswahl is null ? 0 : cmbFilter.Items.IndexOf(auswahl);
            cmbFilter.SelectedIndex = index < 0 ? 0 : index;
            kategorienGeladen = true;
        }
        finally
        {
            filterWirdGeladen = false;
        }
    }

    private void ListeFuellen()
    {
        var gemerkteId = Ausgewaehlt?.Id;

        liste.BeginUpdate();
        liste.Items.Clear();
        foreach (var a in artikel)
        {
            var eintrag = new ListViewItem(a.Name) { Tag = a };
            eintrag.SubItems.Add(a.KategorienText);
            eintrag.SubItems.Add(Ui.Preis(a.Preis));
            eintrag.SubItems.Add(a.Status);
            eintrag.SubItems.Add(a.Datum.ToString("dd.MM.yyyy", Ui.Kultur));
            if (!nurEigene)
            {
                eintrag.SubItems.Add(a.AnbieterName);
            }

            liste.Items.Add(eintrag);
            if (a.Id == gemerkteId)
            {
                eintrag.Selected = true;
            }
        }

        liste.EndUpdate();
    }

    private void DetailsAufbauen()
    {
        details.Dock = DockStyle.Fill;
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        details.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        details.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        details.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        txtBeschreibung.Dock = DockStyle.Fill;
        bild.Dock = DockStyle.Fill;
        details.Controls.Add(txtBeschreibung, 0, 0);
        details.Controls.Add(bild, 1, 0);

        var info = new TableLayoutPanel { ColumnCount = 2, AutoSize = true, Dock = DockStyle.Fill };
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        info.Controls.Add(lblAnbieter, 0, 0);
        info.Controls.Add(lblPreis, 1, 0);
        info.Controls.Add(lblDatum, 0, 1);
        info.Controls.Add(lblStatus, 1, 1);
        info.Controls.Add(lblKategorie, 0, 2);
        details.Controls.Add(info, 0, 1);
        details.SetColumnSpan(info, 2);
    }

    private static Label DetailLabel()
        => new() { AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3, 8, 3, 8) };
}
