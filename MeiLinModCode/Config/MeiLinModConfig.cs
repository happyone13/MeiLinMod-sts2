using BaseLib.Config;

namespace MeiLinMod.MeiLinModCode.Config;

internal class MeiLinModConfig : SimpleModConfig
{
    [ConfigSection("CardVisuals")]
    [ConfigHoverTip]
    public static bool UseChaosCardDynamicPortraits { get; set; } = true;
}
