using TauschboerseBKH.Models;

namespace TauschboerseBKH.Services;

/// <summary>Hält den aktuell angemeldeten Nutzer. <c>null</c> bedeutet Gast.</summary>
internal static class Sitzung
{
    public static Nutzer? Nutzer { get; set; }

    public static bool IstAngemeldet => Nutzer is not null;
}
