using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public static class XiangzuCombatState
{
    public static int GetQi(Creature creature)
    {
        return (int)GetQiAmount(creature);
    }

    public static decimal GetQiAmount(Creature creature)
    {
        return creature.GetPower<QiPower>()?.Amount ?? 0m;
    }

    public static bool HasQi(Creature creature, decimal required = 1m)
    {
        return GetQiAmount(creature) >= required;
    }

    public static bool IsInAttackStance(Creature creature)
    {
        return creature.HasPower<StanceGongPower>() || creature.HasPower<GuiYiDualStancePower>();
    }

    public static bool IsInGuardStance(Creature creature)
    {
        return creature.HasPower<StanceYuPower>() || creature.HasPower<GuiYiDualStancePower>();
    }

    public static bool HasAnyStance(Creature creature)
    {
        return creature.HasPower<StanceGongPower>() ||
               creature.HasPower<StanceYuPower>() ||
               creature.HasPower<GuiYiDualStancePower>();
    }

    public static async Task AddQiProgress(
        Creature owner,
        int progress,
        Creature? applier,
        CardModel? cardSource)
    {
        await QiCounterPower.AddProgress(owner, progress, applier, cardSource);
    }

    public static async Task GainQi(
        Creature owner,
        decimal amount,
        Creature? applier,
        CardModel? cardSource,
        bool silent = false)
    {
        if (amount == 0m)
            return;

        await PowerCmd.Apply<QiPower>(owner, amount, applier ?? owner, cardSource, silent: silent);
    }

    public static async Task<bool> TryConsumeQi(
        Creature owner,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (amount <= 0)
            return true;

        if (!HasQi(owner, amount))
            return false;

        await GainQi(owner, -amount, applier, cardSource);
        return true;
    }

    public static async Task<int> ConsumeAllQi(
        Creature owner,
        Creature? applier,
        CardModel? cardSource)
    {
        var amount = GetQi(owner);
        if (amount <= 0)
            return 0;

        await GainQi(owner, -amount, applier, cardSource);
        return amount;
    }
}
