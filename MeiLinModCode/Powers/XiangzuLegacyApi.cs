using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;

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
        if (power != null)
        {
            await power.SetStance(stance);
            return;
        }

        switch (stance)
        {
            case XiangzuStance.Guard:
                await PowerCmd.Remove<StanceGongPower>(player.Creature);
                await PowerCmd.Apply<StanceYuPower>(player.Creature, 1m, player.Creature, null, silent: true);
                break;
            default:
                await PowerCmd.Remove<StanceYuPower>(player.Creature);
                await PowerCmd.Apply<StanceGongPower>(player.Creature, 1m, player.Creature, null, silent: true);
                break;
        }
    }

    public static async Task ToggleAttackGuard(Player player)
    {
        XiangzuLegacyPower? power = Get(player);
        if (power != null)
        {
            await power.EnterOtherStance();
            return;
        }

        var creature = player.Creature;
        if (creature.HasPower<StanceGongPower>())
        {
            await PowerCmd.Remove<StanceGongPower>(creature);
            await PowerCmd.Apply<StanceYuPower>(creature, 1m, creature, null, silent: true);
            return;
        }

        if (creature.HasPower<StanceYuPower>())
        {
            await PowerCmd.Remove<StanceYuPower>(creature);
            await PowerCmd.Apply<StanceGongPower>(creature, 1m, creature, null, silent: true);
            return;
        }

        await PowerCmd.Apply<StanceGongPower>(creature, 1m, creature, null, silent: true);
    }
}
