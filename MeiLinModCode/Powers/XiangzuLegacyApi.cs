using MegaCrit.Sts2.Core.Entities.Players;

namespace MeiLinMod.MeiLinModCode.Powers;

public static class XiangzuLegacyApi
{
    public static XiangzuLegacyPower? Get(Player player)
    {
        return player.Creature.GetPower<XiangzuLegacyPower>();
    }

    public static void SetTriggerCount(Player player, int count)
    {
        Get(player)?.SetTriggerCount(count);
    }

    public static async Task SetStance(Player player, XiangzuStance stance)
    {
        XiangzuLegacyPower? power = Get(player);
        if (power == null)
            return;

        await power.SetStance(stance);
    }

    public static async Task ToggleAttackGuard(Player player)
    {
        XiangzuLegacyPower? power = Get(player);
        if (power == null)
            return;

        if (player.Creature.HasPower<StanceGongPower>())
            await power.EnterGuardStance();
        else
            await power.EnterAttackStance();
    }
}
