using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MeiLinMod.MeiLinModCode.Cards;

namespace MeiLinMod.MeiLinModCode.Powers;

public class LongYinTemporaryStrengthPower : MeiLinModPower
{
    private decimal _appliedAmount;

    public AbstractModel OriginModel => ModelDb.Card<LongYin>();
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        if (_appliedAmount != 0m || Amount == 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Owner, Amount, Owner, cardSource, silent: true);
        _appliedAmount = Amount;
    }

    public override async Task AfterPowerAmountChanged(
        MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || amount == 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), Owner, amount, Owner, cardSource, silent: true);
        _appliedAmount += amount;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, System.Collections.Generic.IEnumerable<MegaCrit.Sts2.Core.Entities.Creatures.Creature> participants)
    {
        if (side != Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_appliedAmount == 0m)
            return;

        await PowerCmd.Apply<StrengthPower>(new BlockingPlayerChoiceContext(), oldOwner, -_appliedAmount, oldOwner, null, silent: true);
        _appliedAmount = 0m;
    }
}
