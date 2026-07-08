using MeiLinMod.MeiLinModCode.Powers;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Patching.Models;

namespace MeiLinMod.MeiLinModCode.Patches;

public sealed class HuoYongYuXiaCiAfterCombatPatch : IPatchMethod
{
    public static string PatchId => "meilin_huo_yong_yu_xia_ci_after_combat";

    public static bool IsCritical => false;

    public static string Description => "Resolve Huo Yong Yu Xia Ci pending card upgrades after combat";

    public static ModPatchTarget[] GetTargets() =>
    [
        PatchTarget.Method<Player>(nameof(Player.AfterCombatEnd))
    ];

    public static void Postfix(Player __instance)
    {
        var pending = HuoYongYuXiaCiUpgradePower.ConsumePendingUpgrades(__instance);
        if (pending <= 0)
            return;

        var success = 0;
        for (var i = 0; i < pending; i++)
        {
            if (HuoYongYuXiaCiUpgradePower.TryUpgradeRandomCard(__instance))
                success++;
        }

        MainFile.Logger.Info($"[HuoYongYuXiaCi] post-combat resolution: pending={pending}, upgraded={success}.");
    }
}
