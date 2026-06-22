using BaseLib.Config;
using MeiLinMod.MeiLinModCode.Mechanics.CardHoldOverlay;

namespace MeiLinMod.MeiLinModCode.Config;

internal class MeiLinModConfig : SimpleModConfig
{
    private static bool _useBattleReadyOverlay = true;

    [ConfigSection("CardVisuals")]
    [ConfigHoverTip]
    public static bool UseChaosCardDynamicPortraits { get; set; } = true;

    [ConfigSection("CardVisuals")]
    public static bool UseBattleReadyOverlay
    {
        get => _useBattleReadyOverlay;
        set
        {
            _useBattleReadyOverlay = value;
            if (!value)
                MeiLinBattleReadyOverlay.NotifyCombatEnded();
        }
    }

    [ConfigSection("CardVisuals")]
    public static bool UseCombatEffects { get; set; } = true;
}
