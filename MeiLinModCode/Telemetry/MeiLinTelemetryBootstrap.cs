using STS2RitsuLib.Telemetry;
using STS2RitsuLib;

namespace MeiLinMod.MeiLinModCode.Telemetry;

internal static class MeiLinTelemetryBootstrap
{
    private static bool _initialized;

    internal static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;
        TelemetryRegistry.RegisterApplicant(CreateApplicant());
    }

    internal static TelemetryApplicant CreateApplicant()
    {
        return new TelemetryApplicant
        {
            ApplicantId = MeiLinTelemetryConfiguration.ApplicantId,
            OwnerModId = MainFile.ModId,
            DisplayName = MeiLinTelemetryConfiguration.DisplayName,
            Adapter = MeiLinTelemetryConfiguration.CreateAdapter(),
            Requests =
            [
                TelemetryRequest.BasicUsage("Session start, framework/game versions, platform, language, and anonymous install id."),
                TelemetryRequest.ModInventory("Installed mod list, versions, and load states for compatibility analysis."),
                TelemetryRequest.Diagnostics("Exception reports and runtime diagnostics."),
                TelemetryRequest.RunHistory(
                    "Complete MeiLin run history after each run ends, including final deck, outcome, floor reached, ascension, and run duration for balance analysis.",
                    captureFilter: IsMeiLinRun)
            ]
        };
    }

    internal static bool IsMeiLinRun(RunEndedEvent evt)
    {
        return evt.Run.Players.Any(player => IsMeiLinCharacterId(player.CharacterId?.ToString()));
    }

    private static bool IsMeiLinCharacterId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               value.Contains("MEILIN", StringComparison.OrdinalIgnoreCase);
    }
}
