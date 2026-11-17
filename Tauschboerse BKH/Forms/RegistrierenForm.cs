using TauschboerseBKH.Data;
using TauschboerseBKH.Models;
using TauschboerseBKH.Services;

namespace TauschboerseBKH.Forms;

/// <summary>Mockup "Registrieren".</summary>
internal sealed class RegistrierenForm : Form
{
    private const int MindestLaengePasswort = 8;

    private readonly TextBox txtEmail = new() { MaxLength = 100 };
    private readonly ComboBox cmbAnrede = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox txtVorname = new() { MaxLength = 50 };
    private readonly TextBox txtNachname = new() { MaxLength = 50 };
    private readonly TextBox txtTelefon = new() { MaxLength = 30 };
    private readonly TextBox txtPasswort = new() { UseSystemPasswordChar = true, MaxLength = 100 };
    private readonly TextBox txtPasswort2 = new() { UseSystemPasswordChar = true, MaxLength = 100 };

    /// <summary>Die registrierte E-Mail-Adresse (nach erfolgreicher Registrierung).</summary>
    public string Email { get; private set; } = "";

    public RegistrierenForm()
    {
        cmbAnrede.Items.AddRange(new object[] { "Herr", "Frau", "Divers" });
        cmbAnrede.SelectedIndex = 0;

        var tabelle = Ui.Formular();
        Ui.Ueberschrift(tabelle, "Registrieren");
        Ui.Zeile(tabelle, "Anmeldename / E-Mail", txtEmail);
        Ui.Zeile(tabelle, "Anrede", cmbAnrede);
        Ui.Zeile(tabelle, "Vorname", txtVorname);
        Ui.Zeile(tabelle, "Nachname", txtNachname);
        Ui.Zeile(tabelle, "Telefon", txtTelefon);
        Ui.Zeile(tabelle, "Passwort", txtPasswort);
        Ui.Zeile(tabelle, "Passwort wiederholen", txtPasswort2);

        var abbrechen = Ui.Knopf("Abbrechen");
        abbrechen.DialogResult = DialogResult.Cancel;
        var bestaetigen = Ui.Knopf("Bestätigen");
        bestaetigen.Click += (_, _) => Bestaetigen();

        Ui.Dialog(this, "Registrieren", new Size(480, 440), tabelle, Ui.Knopfleiste(null, abbrechen, bestaetigen));
        AcceptButton = bestaetigen;
        CancelButton = abbrechen;
    }

    private void Bestaetigen()
    {
        var email = txtEmail.Text.Trim();
        var vorname = txtVorname.Text.Trim();
        var nachname = txtNachname.Text.Trim();

        if (!MailService.IstGueltigeAdresse(email))
        {
            Ui.Hinweis(this, "Bitte eine gültige E-Mail-Adresse als Anmeldenamen eingeben.");
            return;
        }

        if (vorname.Length == 0 || nachname.Length == 0)
        {
            Ui.Hinweis(this, "Bitte Vor- und Nachname eingeben.");
            return;
        }

        if (txtPasswort.Text.Length < MindestLaengePasswort)
        {
            Ui.Hinweis(this, $"Das Passwort muss mindestens {MindestLaengePasswort} Zeichen lang sein.");
            return;
        }

        if (txtPasswort.Text != txtPasswort2.Text)
        {
            Ui.Hinweis(this, "Die beiden Passwörter stimmen nicht überein.");
            return;
        }

        var nutzer = new Nutzer
        {
            Email = email,
            Anrede = (string)cmbAnrede.SelectedItem!,
            Vorname = vorname,
            Nachname = nachname,
            Telefon = txtTelefon.Text.Trim(),
        };

        try
        {
            if (!NutzerRepository.Registrieren(nutzer, txtPasswort.Text))
            {
                Ui.Fehler(this, "Diese E-Mail-Adresse ist bereits registriert.");
                return;
            }

            Email = email;
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            Ui.Fehler(this, "Die Registrierung ist fehlgeschlagen:\n" + ex.Message);
        }
    }
}
