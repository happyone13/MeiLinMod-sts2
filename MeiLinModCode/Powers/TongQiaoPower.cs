using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using System;

namespace MeiLinMod.MeiLinModCode.Powers;

public class TongQiaoPower : MeiLinModPower
{
    private int _applied;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override int DisplayAmount => (int)Amount;

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        var legacy = Owner.GetPower<XiangzuLegacyPower>();
        if (legacy == null)
            return Task.CompletedTask;

        _applied = Math.Clamp((int)Amount, 1, 2);
        legacy.SetTriggerCount(legacy.TriggerCount - _applied);
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
