using BaseLib.Config;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;
using MeiLinMod.MeiLinModCode.Mechanics.Settings;

namespace MeiLinMod.MeiLinModCode.Config;

internal class MeiLinModConfig : SimpleModConfig
{
    [ConfigSection("CardVisuals")]
    [ConfigHoverTip]
    public static bool UseChaosCardDynamicPortraits
    {
        get => MeiLinSharedSettings.DynamicCardPortraitsEnabled;
        set => MeiLinSharedSettings.SetDynamicCardPortraitsEnabled(value, persist: true);
    }

    [ConfigSection("CardVisuals")]
    public static bool UseBattleReadyOverlay
    {
        get => MeiLinSharedSettings.BattleReadyOverlayEnabled;
        set
        {
            MeiLinSharedSettings.SetBattleReadyOverlayEnabled(value, persist: true);
            if (!value)
                MeiLinBattleReadyOverlay.NotifyCombatEnded();
            else
                MeiLinBattleReadyOverlay.ApplyTransformFromSettings();
        }
    }

    [ConfigSection("CardVisuals")]
    public static bool UseCombatEffects
    {
        get => MeiLinSharedSettings.CombatEffectsEnabled;
        set => MeiLinSharedSettings.SetCombatEffectsEnabled(value, persist: true);
    }
}
