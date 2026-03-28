using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MeiLinMod.MeiLinModCode.Powers;

public class LongXiPower : MeiLinModPower
{
    private bool _adjusting;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (_adjusting || amount <= 0m)
            return;

        if (applier != Owner || power is not EmberPower || power.Owner == Owner)
            return;

        var qi = (int)(Owner.GetPower<QiPower>()?.Amount ?? 0m);
        if (qi <= 0)
            return;

        _adjusting = true;
        try
        {
            await PowerCmd.Apply<EmberPower>(power.Owner, qi, Owner, cardSource, silent: true);
        }
        finally
        {
            _adjusting = false;
        }
    }
}
