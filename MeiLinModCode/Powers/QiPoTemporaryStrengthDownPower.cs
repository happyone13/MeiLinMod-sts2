using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MeiLinMod.MeiLinModCode.Cards;
using MeiLinMod.MeiLinModCode.Extensions;

namespace MeiLinMod.MeiLinModCode.Powers;

public class QiPoTemporaryStrengthDownPower : MeiLinModPower, ICustomModel
{
    private decimal _appliedAmount;

    public AbstractModel OriginModel => ModelDb.Card<QiPo>();
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // Reuse QiPo icon so this debuff is always visible and themed.
    public override string CustomPackedIconPath => "qi_po_power.png".PowerImagePathOrDefault();
    public override string CustomBigIconPath => "qi_po_power.png".BigPowerImagePathOrDefault();

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (_appliedAmount != 0m || Amount == 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(Owner, -Amount, Owner, cardSource, silent: true);
        _appliedAmount = Amount;
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || amount == 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(Owner, -amount, Owner, cardSource, silent: true);
        _appliedAmount += amount;
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_appliedAmount == 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(oldOwner, _appliedAmount, oldOwner, null, silent: true);
        _appliedAmount = 0m;
    }
}
