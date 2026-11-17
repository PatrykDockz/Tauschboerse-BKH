using System.Globalization;

namespace TauschboerseBKH;

/// <summary>Kleine Hilfsmethoden zum Aufbau der Fenster und für Meldungen.</summary>
internal static class Ui
{
    private const string Titel = "Tauschbörse BK-Hilden";

    public static readonly CultureInfo Kultur = CultureInfo.GetCultureInfo("de-DE");

    public static string Preis(decimal preis) => preis.ToString("C2", Kultur);

    public static Button Knopf(string text, int breite = 110)
        => new() { Text = text, Size = new Size(breite, 30), Margin = new Padding(4) };

    public static Font GrosseSchrift(float groesse)
        => new(Control.DefaultFont.FontFamily, groesse, FontStyle.Bold);

    /// <summary>Tabelle mit zwei Spalten (Beschriftung | Eingabe).</summary>
    public static TableLayoutPanel Formular()
    {
        var tabelle = new TableLayoutPanel { ColumnCount = 2, Padding = new Padding(16, 8, 16, 4) };
        tabelle.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        tabelle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return tabelle;
    }

    public static void Ueberschrift(TableLayoutPanel tabelle, string text)
    {
        var zeile = tabelle.RowStyles.Count;
        tabelle.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Font = GrosseSchrift(14f),
            Anchor = AnchorStyles.None,
            Margin = new Padding(3, 8, 3, 12),
        };
        tabelle.Controls.Add(label, 0, zeile);
        tabelle.SetColumnSpan(label, 2);
    }

    /// <param name="hoehe">Feste Höhe für mehrzeilige Eingabefelder (0 = automatisch).</param>
    public static void Zeile(TableLayoutPanel tabelle, string beschriftung, Control eingabe, int hoehe = 0)
    {
        var zeile = tabelle.RowStyles.Count;
        tabelle.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var label = new Label
        {
            Text = beschriftung,
            AutoSize = true,
            Anchor = hoehe > 0 ? AnchorStyles.Top | AnchorStyles.Left : AnchorStyles.Left,
            Margin = new Padding(3, 8, 12, 8),
        };

        eingabe.Dock = DockStyle.Fill;
        eingabe.Margin = new Padding(3, 5, 3, 5);
        if (hoehe > 0)
        {
            eingabe.Height = hoehe;
        }

        tabelle.Controls.Add(label, 0, zeile);
        tabelle.Controls.Add(eingabe, 1, zeile);
    }

    /// <summary>Button-Leiste: optional ein Button links, die übrigen rechts (von links nach rechts angegeben).</summary>
    public static Control Knopfleiste(Button? links, params Button[] rechts)
    {
        var leiste = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(12, 4, 12, 8),
        };
        leiste.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        leiste.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        leiste.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        if (links is not null)
        {
            links.Anchor = AnchorStyles.Left;
            leiste.Controls.Add(links, 0, 0);
        }

        var fluss = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            Anchor = AnchorStyles.Right,
            Margin = Padding.Empty,
        };
        fluss.Controls.AddRange(rechts);
        leiste.Controls.Add(fluss, 1, 0);
        return leiste;
    }

    /// <summary>Richtet ein Dialogfenster mit Inhalt (oben) und Button-Leiste (unten) ein.</summary>
    public static void Dialog(Form fenster, string titel, Size clientGroesse, Control inhalt, Control knoepfe)
    {
        fenster.Text = titel;
        fenster.ClientSize = clientGroesse;
        fenster.FormBorderStyle = FormBorderStyle.FixedDialog;
        fenster.MaximizeBox = false;
        fenster.MinimizeBox = false;
        fenster.StartPosition = FormStartPosition.CenterScreen;

        var wurzel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        wurzel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        wurzel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        wurzel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        inhalt.Dock = DockStyle.Fill;
        knoepfe.Dock = DockStyle.Fill;
        wurzel.Controls.Add(inhalt, 0, 0);
        wurzel.Controls.Add(knoepfe, 0, 1);
        fenster.Controls.Add(wurzel);
    }

    public static void Hinweis(IWin32Window? besitzer, string text)
        => MessageBox.Show(besitzer, text, Titel, MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Fehler(IWin32Window? besitzer, string text)
        => MessageBox.Show(besitzer, text, Titel, MessageBoxButtons.OK, MessageBoxIcon.Error);

    public static bool Frage(IWin32Window? besitzer, string text)
        => MessageBox.Show(besitzer, text, Titel, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
}
