using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MeiLinMod.MeiLinModCode.Powers;

public class EmberPower : MeiLinModPower
{
    private const int TriggerStack = 5;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner)
            return 0m;

        if (!props.HasFlag(ValueProp.Move))
            return 0m;

        // Ember: +1 damage taken per stack.
        return Amount;
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this || amount <= 0m)
            return;

        var previousAmount = Amount - amount;
        var prevTier = (int)decimal.Floor(previousAmount / TriggerStack);
        var newTier = (int)decimal.Floor(Amount / TriggerStack);

        if (newTier <= prevTier)
            return;

        decimal burstDamage = 0m;
        for (var tier = prevTier + 1; tier <= newTier; tier++)
            burstDamage += tier * TriggerStack;

        if (burstDamage <= 0m)
            return;

        Flash();
        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(),
            Owner,
            burstDamage,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Applier,
            null);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        // Expire at round-end (after the opposing side finishes), so self-applied Ember can affect incoming attacks.
        if (side == Owner.Side)
            return;

        if (Owner.HasPower<EmberNoExpireThisTurnPower>())
            return;

        await PowerCmd.Remove(this);
    }
}
