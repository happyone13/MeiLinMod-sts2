using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Powers;

public class LastStandThisTurnPower : MeiLinModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    private bool _consumed;

    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || amount <= 0m)
            return amount;

        var maxLoss = decimal.Max(0m, target.CurrentHp - 1m);
        return decimal.Min(amount, maxLoss);
    }

    public override bool ShouldDie(Creature creature)
    {
        if (_consumed)
            return true;

        if (creature != Owner)
            return true;

        _consumed = true;
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner)
            return;

        if (Owner.CurrentHp <= 0m)
            await CreatureCmd.Heal(Owner, 1m, playAnim: false);

        await PowerCmd.Remove(this);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // Keep this protection through the opponent's turn, then clear it.
        if (side == Owner.Side)
            return;

        await PowerCmd.Remove(this);
    }
}
