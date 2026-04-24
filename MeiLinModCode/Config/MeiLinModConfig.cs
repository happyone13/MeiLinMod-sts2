using BaseLib.Config;

namespace MeiLinMod.MeiLinModCode.Config;

internal class MeiLinModConfig : SimpleModConfig
{
    [ConfigSection("CardVisuals")]
    [ConfigHideInUI]
    [ConfigHoverTip]
    public static bool UseChaosCardDynamicPortraits { get; set; } = false;
}
