using TauschboerseBKH.Controls;
using TauschboerseBKH.Services;

namespace TauschboerseBKH.Forms;

/// <summary>Mockup "Zentrales Fenster" mit den Reitern Menü, Suchen und Meine Artikel.</summary>
internal sealed class HauptForm : Form
{
    private readonly TabControl reiter = new() { Dock = DockStyle.Fill };
    private readonly SuchenBereich suchen = new();
    private readonly MeineArtikelBereich? meineArtikel;

    /// <summary>Wahr, wenn der Nutzer sich abgemeldet hat und zur Anmeldung zurückkehren soll.</summary>
    public bool Abgemeldet { get; private set; }

    public HauptForm()
    {
        Text = "Tauschbörse BK-Hilden";
        ClientSize = new Size(1000, 650);
        MinimumSize = new Size(820, 520);
        StartPosition = FormStartPosition.CenterScreen;

        var reiterMenue = new TabPage("Menü");
        reiterMenue.Controls.Add(MenueErstellen());

        var reiterSuchen = new TabPage("Suchen");
        reiterSuchen.Controls.Add(suchen);

        reiter.TabPages.Add(reiterMenue);
        reiter.TabPages.Add(reiterSuchen);

        // Gäste dürfen keine eigenen Artikel verwalten
        if (Sitzung.IstAngemeldet)
        {
            meineArtikel = new MeineArtikelBereich();
            var reiterMeine = new TabPage("Meine Artikel");
            reiterMeine.Controls.Add(meineArtikel);
            reiter.TabPages.Add(reiterMeine);
        }

        Controls.Add(reiter);
        reiter.SelectedTab = reiterSuchen;
        reiter.SelectedIndexChanged += (_, _) => AktuellenReiterAktualisieren();
        Shown += (_, _) => AktuellenReiterAktualisieren();
    }

    private void AktuellenReiterAktualisieren()
    {
        if (reiter.SelectedIndex == 1)
        {
            suchen.Aktualisieren(true);
        }
        else if (reiter.SelectedIndex == 2)
        {
            meineArtikel?.Aktualisieren(true);
        }
    }

    private Control MenueErstellen()
    {
        var nutzer = Sitzung.Nutzer;

        var titel = new Label
        {
            Text = "Tauschbörse BK-Hilden",
            AutoSize = true,
            Font = Ui.GrosseSchrift(18f),
            Margin = new Padding(0, 0, 0, 12),
        };

        var info = new Label
        {
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 20),
            Text = nutzer is null
                ? "Sie sind als Gast angemeldet. Zum Einstellen eigener Artikel bitte registrieren und anmelden."
                : $"Angemeldet als: {nutzer.VollerName} ({nutzer.Email})",
        };

        var abmelden = Ui.Knopf(nutzer is null ? "Anmelden / Registrieren" : "Abmelden", 200);
        abmelden.Click += (_, _) =>
        {
            Abgemeldet = true;
            Close();
        };

        var beenden = Ui.Knopf("Beenden", 200);
        beenden.Click += (_, _) => Close();

        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(30),
        };
        panel.Controls.AddRange(new Control[] { titel, info, abmelden, beenden });
        return panel;
    }
}
