using Godot;
using Godot.Bridge;
using MegaCrit.Sts2.Core.Modding;
using MeiLinMod.MeiLinModCode.Migration;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;
using MeiLinMod.MeiLinModCode.Patches;
using MeiLinMod.MeiLinModCode.StanceVfx;
using MeiLinMod.MeiLinModCode.Telemetry;
using MeiLinMod.MeiLinModCode.Vfx;

namespace MeiLinMod;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "MeiLinMod";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        var assembly = typeof(MainFile).Assembly;
        MeiLinRitsuMigration.Initialize();
        MeiLinTelemetryBootstrap.Initialize();
        ScriptManagerBridge.LookupScriptsInAssembly(assembly);
        MeiLinSharedSettings.EnsureSettingsLoaded();
        GloomyEncounterSharedSettings.RegisterProvider(ModId);
        CardSpinePortraitPatch.PreloadDynamicPortraitScenes();
        MeiLinCommandVfxCoordinator.PreloadConfiguredScenes();
        MeiLinAttackMovementController.PreloadMovementEffects();
        MeiLinStanceVfxController.PreloadStanceEffects();
    }
}
