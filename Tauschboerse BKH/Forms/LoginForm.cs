using TauschboerseBKH.Data;
using TauschboerseBKH.Services;

namespace TauschboerseBKH.Forms;

/// <summary>Mockup "Login": Anmeldename, Passwort, Als Gast einloggen, Registrieren, Bestätigen.</summary>
internal sealed class LoginForm : Form
{
    private readonly TextBox txtEmail = new() { MaxLength = 100 };
    private readonly TextBox txtPasswort = new() { UseSystemPasswordChar = true, MaxLength = 100 };

    public LoginForm()
    {
        var tabelle = Ui.Formular();
        Ui.Zeile(tabelle, "Anmeldename", txtEmail);
        Ui.Zeile(tabelle, "Passwort", txtPasswort);

        var gast = Ui.Knopf("Als Gast einloggen", 140);
        var registrieren = Ui.Knopf("Registrieren");
        var bestaetigen = Ui.Knopf("Bestätigen");

        gast.Click += (_, _) =>
        {
            Sitzung.Nutzer = null;
            DialogResult = DialogResult.OK;
        };
        registrieren.Click += (_, _) => Registrieren();
        bestaetigen.Click += (_, _) => Anmelden();

        Ui.Dialog(this, "Tauschbörse BK-Hilden – Anmeldung", new Size(480, 170), tabelle,
            Ui.Knopfleiste(gast, registrieren, bestaetigen));
        AcceptButton = bestaetigen;
    }

    private void Anmelden()
    {
        var email = txtEmail.Text.Trim();
        if (email.Length == 0 || txtPasswort.Text.Length == 0)
        {
            Ui.Hinweis(this, "Bitte Anmeldename und Passwort eingeben.");
            return;
        }

        try
        {
            var nutzer = NutzerRepository.Anmelden(email, txtPasswort.Text);
            if (nutzer is null)
            {
                Ui.Fehler(this, "Anmeldename oder Passwort ist falsch.");
                txtPasswort.Clear();
                return;
            }

            Sitzung.Nutzer = nutzer;
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            Ui.Fehler(this, "Die Anmeldung ist fehlgeschlagen:\n" + ex.Message);
        }
    }

    private void Registrieren()
    {
        using var fenster = new RegistrierenForm();
        if (fenster.ShowDialog(this) == DialogResult.OK)
        {
            txtEmail.Text = fenster.Email;
            txtPasswort.Clear();
            txtPasswort.Focus();
            Ui.Hinweis(this, "Die Registrierung war erfolgreich. Sie können sich jetzt anmelden.");
        }
    }
}
