using TauschboerseBKH.Forms;
using TauschboerseBKH.Models;
using TauschboerseBKH.Services;

namespace TauschboerseBKH.Controls;

/// <summary>Reiter "Suchen": Artikelliste, Details und Kontaktaufnahme zum Anbieter.</summary>
internal sealed class SuchenBereich : ArtikelBereich
{
    private readonly Button btnDetails = Ui.Knopf("Details");
    private readonly Button btnZurueck = Ui.Knopf("Zurück");
    private readonly Button btnKontakt = Ui.Knopf("Kontaktieren", 120);

    public SuchenBereich() : base(nurEigene: false)
    {
        btnZurueck.Visible = false;
        btnKontakt.Visible = false;

        btnDetails.Click += (_, _) => DetailsOeffnen();
        btnZurueck.Click += (_, _) => ZeigeListe();
        btnKontakt.Click += (_, _) => Kontaktieren();

        KnoepfeHinzufuegen(btnZurueck, btnDetails, btnKontakt);
    }

    public override void ZeigeListe()
    {
        base.ZeigeListe();
        btnDetails.Visible = true;
        btnZurueck.Visible = false;
        btnKontakt.Visible = false;
    }

    public override void ZeigeDetails(Artikel a)
    {
        base.ZeigeDetails(a);
        btnDetails.Visible = false;
        btnZurueck.Visible = true;
        btnKontakt.Visible = true;
    }

    protected override void ArtikelDoppelklick() => DetailsOeffnen();

    private void DetailsOeffnen()
    {
        var a = Ausgewaehlt;
        if (a is null)
        {
            Ui.Hinweis(FindForm(), "Bitte zuerst einen Artikel in der Liste auswählen.");
            return;
        }

        ZeigeDetails(a);
    }

    private void Kontaktieren()
    {
        var a = Ausgewaehlt;
        if (a is null)
        {
            return;
        }

        if (Sitzung.Nutzer?.Id == a.NutzerId)
        {
            Ui.Hinweis(FindForm(), "Dies ist Ihr eigener Artikel.");
            return;
        }

        using var fenster = new KontaktForm(a);
        fenster.ShowDialog(FindForm());
    }
}
