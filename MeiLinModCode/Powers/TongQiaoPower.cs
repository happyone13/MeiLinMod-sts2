using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class TongQiaoPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override int DisplayAmount => (int)Amount;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await QiCounterPower.ResolvePending(Owner, Owner, cardSource);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await QiCounterPower.ResolvePending(oldOwner, oldOwner, null);
    }
}
