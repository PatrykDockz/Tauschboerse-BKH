using TauschboerseBKH.Forms;
using TauschboerseBKH.Services;

namespace TauschboerseBKH;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        bool erneutAnmelden;
        do
        {
            Sitzung.Nutzer = null;

            using (var login = new LoginForm())
            {
                if (login.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            using var haupt = new HauptForm();
            Application.Run(haupt);
            erneutAnmelden = haupt.Abgemeldet;
        }
        while (erneutAnmelden);
    }
}
