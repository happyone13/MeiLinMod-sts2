using System;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class XiangzuSpiritPower : MeiLinModPower
{
    private int _applied;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override int DisplayAmount => (int)Amount;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var legacy = Owner.GetPower<XiangzuLegacyPower>();
        if (legacy == null)
            return Task.CompletedTask;

        var targetApplied = Math.Max(1, (int)Amount);
        var delta = targetApplied - _applied;
        if (delta != 0)
        {
            legacy.SetTriggerCount(legacy.TriggerCount - delta);
            _applied = targetApplied;
        }

        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || power.Owner != Owner)
            return Task.CompletedTask;

        var legacy = Owner.GetPower<XiangzuLegacyPower>();
        if (legacy == null)
            return Task.CompletedTask;

        var targetApplied = Math.Max(1, (int)Amount);
        var delta = targetApplied - _applied;
        if (delta != 0)
        {
            legacy.SetTriggerCount(legacy.TriggerCount - delta);
            _applied = targetApplied;
        }

        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        var legacy = oldOwner.GetPower<XiangzuLegacyPower>();
        if (legacy != null && _applied != 0)
            legacy.SetTriggerCount(legacy.TriggerCount + _applied);

        return Task.CompletedTask;
    }
}
