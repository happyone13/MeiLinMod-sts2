using MegaCrit.Sts2.Core.Nodes.Rooms;
using MeiLinMod.MeiLinModCode.Vfx;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class MeiLinBattleVfxWarmPatch : IPatchMethod
{
    public static string PatchId => "meilin_battle_vfx_deep_warm";

    public static bool IsCritical => false;

    public static string Description => "Deep-warm frequent MeiLin VFX across battle frames";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<NCombatRoom>(nameof(NCombatRoom._Ready))
    ];

    public static void Postfix(NCombatRoom __instance)
    {
        MeiLinBattleVfxPrewarmer.Start(__instance);
    }
}
