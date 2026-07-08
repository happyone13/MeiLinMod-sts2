using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class YukiSettingsPanelEmptyReadyCompatPatch : IPatchMethod
{
    private const string YukiModPanelName = "XCskin_ModSettingsPanel";

    public static string PatchId => "meilin_yuki_settings_empty_panel_ready_compat";
    public static bool IsCritical => false;
    public static string Description => "Suppress the transient empty Yuki settings panel _Ready exception when YukiMod is loaded with RitsuLib";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NSettingsPanel>(nameof(NSettingsPanel._Ready))
    ];

    public static Exception? Finalizer(NSettingsPanel __instance, Exception? __exception)
    {
        if (__exception == null)
        {
            return null;
        }

        if (!IsTransientYukiEmptyPanelReadyException(__instance, __exception))
        {
            return __exception;
        }

        MainFile.Logger.Warn("[YukiSettingsPanelEmptyReadyCompatPatch] Suppressed transient empty Yuki settings panel _Ready exception.");
        return null;
    }

    private static bool IsTransientYukiEmptyPanelReadyException(NSettingsPanel panel, Exception exception)
    {
        if (exception is not InvalidOperationException || exception.Message != "Sequence contains no elements")
        {
            return false;
        }

        if (!string.Equals(panel.Name.ToString(), YukiModPanelName, StringComparison.Ordinal))
        {
            return false;
        }

        var vbox = panel.GetNodeOrNull<VBoxContainer>("VBoxContainer");
        return vbox == null || vbox.GetChildCount() == 0;
    }
}
