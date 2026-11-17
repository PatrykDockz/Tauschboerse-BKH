using TauschboerseBKH.Data;
using TauschboerseBKH.Forms;
using TauschboerseBKH.Services;

namespace TauschboerseBKH.Controls;

/// <summary>Reiter "Meine Artikel": eigene Artikel anlegen, bearbeiten und löschen.</summary>
internal sealed class MeineArtikelBereich : ArtikelBereich
{
    private readonly Button btnNeu = Ui.Knopf("Neuer Artikel", 120);
    private readonly Button btnBearbeiten = Ui.Knopf("Bearbeiten");
    private readonly Button btnLoeschen = Ui.Knopf("Artikel löschen", 130);

    public MeineArtikelBereich() : base(nurEigene: true)
    {
        btnNeu.Click += (_, _) => Neu();
        btnBearbeiten.Click += (_, _) => Bearbeiten();
        btnLoeschen.Click += (_, _) => Loeschen();

        KnoepfeHinzufuegen(btnNeu, btnBearbeiten, btnLoeschen);
    }

    protected override void ArtikelDoppelklick() => Bearbeiten();

    private void Neu()
    {
        using var fenster = new ArtikelForm(null);
        if (fenster.ShowDialog(FindForm()) == DialogResult.OK)
        {
            Aktualisieren(kategorienNeuLaden: true);
        }
    }

    private void Bearbeiten()
    {
        var a = Ausgewaehlt;
        if (a is null)
        {
            Ui.Hinweis(FindForm(), "Bitte zuerst einen Artikel in der Liste auswählen.");
            return;
        }

        using var fenster = new ArtikelForm(a);
        if (fenster.ShowDialog(FindForm()) == DialogResult.OK)
        {
            Aktualisieren(kategorienNeuLaden: true);
        }
    }

    private void Loeschen()
    {
        var a = Ausgewaehlt;
        if (a is null)
        {
            Ui.Hinweis(FindForm(), "Bitte zuerst einen Artikel in der Liste auswählen.");
            return;
        }

        if (!Ui.Frage(FindForm(), $"Soll der Artikel \"{a.Name}\" wirklich gelöscht werden?"))
        {
            return;
        }

        try
        {
            ArtikelRepository.Loeschen(a.Id, Sitzung.Nutzer!.Id);
            FotoService.Loeschen(a.Foto);
            Aktualisieren(kategorienNeuLaden: true);
        }
        catch (Exception ex)
        {
            Ui.Fehler(FindForm(), "Der Artikel konnte nicht gelöscht werden:\n" + ex.Message);
        }
    }
}
